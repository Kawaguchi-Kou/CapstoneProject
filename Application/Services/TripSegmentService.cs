using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Weather;
using static System.Formats.Asn1.AsnWriter;

namespace Application.Services
{
    public class TripSegmentService : ITripSegmentService
    {
        private readonly ITripSegmentRepository _segmentRepo;
        private readonly ILocationRepository _locationRepo;
        private readonly IWeatherForecastRepository _weatherRepo;
        private readonly IAdaptiveWeatherRiskEngine _riskEngine;
        private readonly ITripRepository _tripRepo;
        private readonly IGeocodingService _geocodingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistrictRepository _districtRepo;
        private readonly IRouteGraphService _routeGraph;
        private readonly IWeatherService _weatherService;
        private readonly IGeminiService _gemini;

        public TripSegmentService(
            ITripSegmentRepository segmentRepo,
            ILocationRepository locationRepository,
            IWeatherForecastRepository weatherForecastRepository,
            IAdaptiveWeatherRiskEngine adaptiveWeatherRiskEngine,
            ITripRepository tripRepo,
            IGeocodingService geocodingService,
            IUnitOfWork unitOfWork,
            IDistrictRepository districtRepo,
            IRouteGraphService routeGraph,
            IWeatherService weatherService,
            IGeminiService gemini)
        {
            _segmentRepo      = segmentRepo;
            _locationRepo     = locationRepository;
            _weatherRepo      = weatherForecastRepository;
            _riskEngine       = adaptiveWeatherRiskEngine;
            _tripRepo         = tripRepo;
            _geocodingService = geocodingService;
            _unitOfWork       = unitOfWork;
            _districtRepo     = districtRepo;
            _routeGraph       = routeGraph;
            _weatherService   = weatherService;
            _gemini           = gemini;
        }

        public async Task<List<Location>> RecommendSegmentsAsync(
            DateTime startDate,
            DateTime endDate,
            int maxStops)
        {
            var locations = await _locationRepo.GetAllAsync();

            var scored = new List<(Location loc, double score)>();

            foreach (var loc in locations)
            {
                var forecast = await _weatherRepo.GetAsync(loc.LocationId, startDate);

                var risk = _riskEngine.CalculateRisk(forecast!);

                var score = 1 - risk; // risk thấp = tốt

                scored.Add((loc, score));
            }

            return scored
                .OrderByDescending(x => x.score)
                .Take(maxStops)
                .Select(x => x.loc)
                .ToList();
        }

        public async Task<List<TripSegment>> InsertSegmentsAsync(
    Guid tripId,
    int insertAt,
    List<TripSegment> newSegments)
        {
            // 1. Validate trip
            var trip = await _tripRepo.GetByIdAsync(tripId);
            if (trip == null)
                throw new Exception("Trip not found");

            if (newSegments == null || !newSegments.Any())
                throw new Exception("Segments cannot be empty");

            var existing = trip.TripSegments
                .OrderBy(x => x.OrderIndex)
                .ToList();

            if (!existing.Any())
                throw new Exception("Trip has no base segments");

            // 2. Validate insert position
            if (insertAt < 1 || insertAt > existing.Count)
                throw new Exception("Invalid insert position");

            // 3. Identify prev & next BEFORE shifting
            var prevSegment = existing
                .FirstOrDefault(x => x.OrderIndex == insertAt - 1);

            var nextSegment = existing
                .FirstOrDefault(x => x.OrderIndex == insertAt);

            int shift = newSegments.Count;

            // 4. Shift existing segments
            foreach (var seg in existing.Where(x => x.OrderIndex >= insertAt))
            {
                seg.OrderIndex += shift;
            }

            // 5. Prepare location preload
            var locationIds = newSegments.Select(x => x.LocationId).ToList();

            if (prevSegment != null)
                locationIds.Add(prevSegment.LocationId);

            if (nextSegment != null)
                locationIds.Add(nextSegment.LocationId);

            locationIds = locationIds.Distinct().ToList();

            var locationDict = await _locationRepo
                .GetByIdsAsDictionaryAsync(locationIds);

            // 6. Resolve previous location
            Location? prevLocation = null;

            if (prevSegment != null)
            {
                if (!locationDict.ContainsKey(prevSegment.LocationId))
                    throw new Exception("Previous location not found");

                prevLocation = locationDict[prevSegment.LocationId];
            }

            // 7. Insert new segments + calculate distance
            int index = 0;

            foreach (var segment in newSegments)
            {
                if (!locationDict.ContainsKey(segment.LocationId))
                    throw new Exception($"Location {segment.LocationId} not found");

                if (!locationDict.ContainsKey(segment.LocationId))
                    throw new Exception($"Location {segment.LocationId} not found");

                // 🔥 VALIDATE DISTRICT
                var district = await _districtRepo.GetByIdAsync(segment.DistrictId);

                if (district == null)
                    throw new Exception($"District {segment.DistrictId} not found");

                if (district.LocationId != segment.LocationId)
                    throw new Exception($"District does not belong to location");

                var currentLocation = locationDict[segment.LocationId];

                segment.SegmentId = Guid.NewGuid();
                segment.TripId = tripId;
                segment.CreatedAt = DateTime.UtcNow;
                segment.OrderIndex = insertAt + index;

                if (prevLocation == null)
                {
                    segment.DistanceKm = 0;
                }
                else
                {
                    segment.DistanceKm = await _geocodingService.GetDrivingDistance(
                        prevLocation.Latitude, prevLocation.Longitude,
                        currentLocation.Latitude, currentLocation.Longitude
                    );
                }

                prevLocation = currentLocation;
                index++;
            }

            // 8. Fix distance for the NEXT segment (critical)
            //if (nextSegment != null && prevLocation != null)
            //{
            //    if (!locationDict.ContainsKey(nextSegment.LocationId))
            //        throw new Exception("Next location not found");

            //    var nextLocation = locationDict[nextSegment.LocationId];

            //    nextSegment.DistanceKm = await _geocodingService.GetDrivingDistance(
            //        prevLocation.Latitude, prevLocation.Longitude,
            //        nextLocation.Latitude, nextLocation.Longitude
            //    );
            //}

            // reload ALL segments (including new ones)
            var allSegments = await _segmentRepo.GetByTripIdAsync(tripId);

            // normalize order (important after shift)
            var ordered = allSegments.OrderBy(x => x.OrderIndex).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].OrderIndex = i + 1;
            }

            // 🔥 recalc everything
            await RecalculateDistances(ordered);

            // 9. Save new segments
            await _segmentRepo.AddRangeAsync(newSegments);

            return newSegments;
        }

        public async Task UpdateSegmentDatesAsync(
    Guid tripId,
    List<UpdateSegmentDatesRequest> updates)
        {
            if (updates == null || !updates.Any())
                throw new ArgumentException("Update list cannot be empty");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null)
                    throw new Exception("Trip not found");

                var segments = await _segmentRepo.GetByTripIdAsync(tripId);

                if (segments == null || !segments.Any())
                    throw new Exception("No segments found");

                var segmentDict = segments.ToDictionary(x => x.SegmentId);

                foreach (var update in updates)
                {
                    if (!segmentDict.ContainsKey(update.SegmentId))
                        throw new Exception($"Segment {update.SegmentId} not found");

                    var seg = segmentDict[update.SegmentId];

                    seg.StartDate = update.StartDate;
                    seg.EndDate = update.EndDate;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteSegmentsAsync(
    Guid tripId,
    List<Guid> segmentIds)
        {
            if (segmentIds == null || !segmentIds.Any())
                throw new ArgumentException("SegmentIds cannot be empty");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var trip = await _tripRepo.GetByIdAsync(tripId);
                if (trip == null)
                    throw new Exception("Trip not found");

                var segments = await _segmentRepo.GetByTripIdAsync(tripId);

                if (segments == null || !segments.Any())
                    throw new Exception("No segments found");

                var toDelete = segments
                    .Where(x => segmentIds.Contains(x.SegmentId))
                    .ToList();

                if (!toDelete.Any())
                    throw new Exception("No matching segments to delete");

                if (toDelete.Count == segments.Count)
                    throw new Exception("Cannot delete all segments");

                // 🔹 Delete by IDs (repo)
                await _segmentRepo.DeleteByIdsAsync(segmentIds);

                // 🔹 Remaining segments (already in memory)
                var remaining = segments
                    .Where(x => !segmentIds.Contains(x.SegmentId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                // 🔹 Reorder
                for (int i = 0; i < remaining.Count; i++)
                {
                    remaining[i].OrderIndex = i + 1;
                }

                // 🔹 Distance recalculation
                var locationIds = remaining.Select(x => x.LocationId).Distinct().ToList();
                var locationDict = await _locationRepo.GetByIdsAsDictionaryAsync(locationIds);

                for (int i = 0; i < remaining.Count; i++)
                {
                    if (i == 0)
                    {
                        remaining[i].DistanceKm = 0;
                        continue;
                    }

                    var prev = remaining[i - 1];
                    var curr = remaining[i];

                    var prevLoc = locationDict[prev.LocationId];
                    var currLoc = locationDict[curr.LocationId];

                    curr.DistanceKm = await _geocodingService.GetDrivingDistance(
                        prevLoc.Latitude, prevLoc.Longitude,
                        currLoc.Latitude, currLoc.Longitude
                    );
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Location>> GetAllAsync()
        {
            var locations = await _locationRepo.GetAllAsync();
            return locations;
        }

        // ====================================================
        // ROUTE SUGGESTIONS
        // ====================================================
        //public async Task<List<RouteSuggestionResponse>> GetRouteSuggestionsAsync(Guid tripId)
        //{
        //    // 1. Load trip
        //    var trip = await _tripRepo.GetByIdAsync(tripId)
        //        ?? throw new Exception("Trip not found");

        //    if (string.IsNullOrWhiteSpace(trip.StartLocation) ||
        //        string.IsNullOrWhiteSpace(trip.EndLocation))
        //        throw new Exception("Trip start or end location is not set");

        //    // 2. Find top-5 paths via Yen’s algorithm
        //    var paths = _routeGraph.FindTopKPaths(
        //        trip.StartLocation,
        //        trip.EndLocation,
        //        k: 5);

        //    if (!paths.Any())
        //        return new List<RouteSuggestionResponse>();

        //    // 3. Preload all DB locations (one query) to match graph nodes by name
        //    var allLocations = await _locationRepo.GetAllAsync();
        //    var locationByName = allLocations
        //        .GroupBy(l => l.LocationName.Trim(), StringComparer.OrdinalIgnoreCase)
        //        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        //    // Trip date range for weather preloading
        //    var tripDates = Enumerable
        //        .Range(0, Math.Max(1, (trip.EndDate.Date - trip.StartDate.Date).Days + 1))
        //        .Select(i => trip.StartDate.Date.AddDays(i))
        //        .ToList();

        //    // 4. Preload weather for every unique node that has a DB location
        //    //    Uses GetRangeOptimizedAsync — one Open-Meteo call per location
        //    var uniqueNodeIds = paths
        //        .SelectMany(p => p.Nodes)
        //        .Distinct(StringComparer.OrdinalIgnoreCase)
        //        .ToList();

        //    var weatherByLocationId = new Dictionary<Guid, Dictionary<DateTime, WeatherForecast>>();
        //    // map: graphNodeId → DB Location
        //    var nodeToLocation = new Dictionary<string, Location?>(StringComparer.OrdinalIgnoreCase);

        //    foreach (var nodeId in uniqueNodeIds)
        //    {
        //        var graphNode = _routeGraph.Nodes.TryGetValue(nodeId, out var gn) ? gn : null;
        //        if (graphNode == null) { nodeToLocation[nodeId] = null; continue; }

        //        // Try matching by graph label or graph id
        //        Location? dbLoc = null;
        //        if (locationByName.TryGetValue(graphNode.Label, out var byLabel))
        //            dbLoc = byLabel;
        //        else if (locationByName.TryGetValue(graphNode.Id, out var byId))
        //            dbLoc = byId;

        //        nodeToLocation[nodeId] = dbLoc;

        //        if (dbLoc != null && !weatherByLocationId.ContainsKey(dbLoc.LocationId))
        //        {
        //            // One batched Open-Meteo call covers the full date range
        //            var forecasts = await _weatherService
        //                .GetRangeOptimizedAsync(dbLoc.LocationId, tripDates);
        //            weatherByLocationId[dbLoc.LocationId] = forecasts;
        //        }
        //    }

        //    // 5. Build response for each route
        //    var result = new List<RouteSuggestionResponse>();

        //    for (int ri = 0; ri < paths.Count; ri++)
        //    {
        //        var path     = paths[ri];
        //        var stops    = new List<RouteStopDto>();
        //        var edgeMap  = new Dictionary<string, GraphEdge>(); // target → edge

        //        foreach (var edge in path.Edges)
        //            edgeMap[edge.Target] = edge;

        //        foreach (var nodeId in path.Nodes)
        //        {
        //            var gn       = _routeGraph.Nodes.TryGetValue(nodeId, out var gnn) ? gnn : null;
        //            var edge     = edgeMap.TryGetValue(nodeId, out var e) ? e : null;
        //            var dbLoc    = nodeToLocation.TryGetValue(nodeId, out var dl) ? dl : null;

        //            WeatherSnapshotDto? weatherDto = null;

        //            if (dbLoc != null &&
        //                weatherByLocationId.TryGetValue(dbLoc.LocationId, out var forecasts) &&
        //                forecasts.Any())
        //            {
        //                // Use first available forecast date (trip start)
        //                var firstForecast = forecasts
        //                    .OrderBy(kv => kv.Key)
        //                    .First().Value;

        //                weatherDto = new WeatherSnapshotDto
        //                {
        //                    TemperatureCelsius       = firstForecast.TemperatureCelsius,
        //                    PrecipitationProbability = firstForecast.PrecipitationProbability,
        //                    WindSpeed                = firstForecast.WindSpeed
        //                };
        //            }

        //            stops.Add(new RouteStopDto
        //            {
        //                NodeId              = nodeId,
        //                Label               = gn?.Label ?? nodeId,
        //                DistanceFromPrevKm  = edge?.DistanceKm ?? 0,
        //                RouteType           = edge?.RouteType  ?? string.Empty,
        //                Weather             = weatherDto
        //            });
        //        }

        //        // 6. Build weather digest text
        //        var weatherSummary = BuildWeatherSummary(stops);

        //        // 7. Call Gemini for Vietnamese recommendation
        //        var aiRec = await GetAiRecommendationAsync(
        //            trip.StartLocation,
        //            trip.EndLocation,
        //            trip.StartDate,
        //            stops,
        //            path.TotalDistanceKm);

        //        result.Add(new RouteSuggestionResponse
        //        {
        //            RouteIndex        = ri + 1,
        //            Stops             = stops,
        //            TotalDistanceKm   = path.TotalDistanceKm,
        //            WeatherSummary    = weatherSummary,
        //            AiRecommendation  = aiRec
        //        });
        //    }

        //    return result;
        //}

        public async Task<List<RouteSuggestionResponse>>
    GetRouteSuggestionsAsync(Guid tripId)
        {
            // ============================================
            // 1. Load trip segments
            // ============================================

            var segments = (await _segmentRepo
                .GetByTripIdAsync(tripId))
                .OrderBy(x => x.OrderIndex)
                .ToList();

            if (segments.Count < 2)
                throw new Exception("Trip must contain start/end segments.");

            var startSegment = segments.First();
            var endSegment = segments.Last();

            var startLocation = await _locationRepo
                .GetByIdAsync(startSegment.LocationId);

            var endLocation = await _locationRepo
                .GetByIdAsync(endSegment.LocationId);

            if (startLocation == null || endLocation == null)
                throw new Exception("Start/end location not found");

            // check graph mapping using location name
            if (!_routeGraph.NodeExists(startLocation.LocationName) ||
                !_routeGraph.NodeExists(endLocation.LocationName))
            {
                throw new Exception("Graph node mapping missing");
            }

            // ============================================
            // 2. Find graph routes
            // ============================================

            var paths = _routeGraph.FindTopKPaths(
                startLocation.LocationName,
                endLocation.LocationName,
                5);

            if (!paths.Any())
                return new List<RouteSuggestionResponse>();
            // ============================================
            // 3. Preload locations
            // ============================================

            var allLocations = await _locationRepo.GetAllAsync();

            var locationByName = allLocations
                .GroupBy(x => x.LocationName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

            // ============================================
            // 4. Trip dates
            // ============================================

            var trip = await _tripRepo.GetByIdAsync(tripId)
                ?? throw new Exception("Trip not found");

            var tripDates = Enumerable
                .Range(0,
                    Math.Max(1,
                        (trip.EndDate.Date - trip.StartDate.Date).Days + 1))
                .Select(i => trip.StartDate.Date.AddDays(i))
                .ToList();
            // ============================================
            // 5. Preload weather
            // ============================================

            var uniqueNodeIds = paths
                .SelectMany(x => x.Nodes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

            var nodeToLocation =
                new Dictionary<string, Location?>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var nodeId in uniqueNodeIds)
            {
                var graphNode = _routeGraph.Nodes
                    .TryGetValue(nodeId, out var gn)
                        ? gn
                        : null;

                if (graphNode == null)
                {
                    nodeToLocation[nodeId] = null;
                    continue;
                }

                Location? dbLoc = null;

                if (locationByName.TryGetValue(graphNode.Label, out var byLabel))
                    dbLoc = byLabel;
                else if (locationByName.TryGetValue(graphNode.Id, out var byId))
                    dbLoc = byId;

                nodeToLocation[nodeId] = dbLoc;
            }

            var weatherByLocationId =
                new Dictionary<Guid, Dictionary<DateTime, WeatherForecast>>();

            var validLocations = nodeToLocation
                .Values
                .Where(x => x != null)
                .DistinctBy(x => x!.LocationId)
                .Select(x => x!)
                .ToList();

            /*var weatherTasks = validLocations.Select(async loc =>
            //{
            //    var forecasts = await _weatherService
            //        .GetRangeOptimizedAsync(loc.LocationId, tripDates);

            //    return (loc.LocationId, forecasts);
            //});

            //var weatherResults = await Task.WhenAll(weatherTasks);

            //foreach (var result in weatherResults)
            //{
            //    weatherByLocationId[result.LocationId] = result.forecasts;
            }
        */

            foreach (var loc in validLocations)
            {
                var forecasts = await _weatherService
                    .GetRangeOptimizedAsync(loc.LocationId, tripDates);
    
                weatherByLocationId[loc.LocationId] = forecasts;
            }

            // ============================================
            // 6. Build responses
            // ============================================

            var responses = new List<RouteSuggestionResponse>();

            foreach (var path in paths)
            {
                var stops = new List<RouteStopDto>();

                var edgeMap = new Dictionary<string, GraphEdge>();

                foreach (var edge in path.Edges)
                    edgeMap[edge.Target] = edge;

                foreach (var nodeId in path.Nodes)
                {
                    var graphNode = _routeGraph.Nodes
                        .TryGetValue(nodeId, out var gn)
                            ? gn
                            : null;

                    var edge = edgeMap.TryGetValue(nodeId, out var e)
                        ? e
                        : null;

                    var dbLoc = nodeToLocation.TryGetValue(nodeId, out var loc)
                        ? loc
                        : null;

                    WeatherSnapshotDto? weather = null;

                    if (dbLoc != null &&
                        weatherByLocationId.TryGetValue(dbLoc.LocationId, out var forecasts) &&
                        forecasts.Any())
                    {
                        var firstForecast = forecasts
                            .OrderBy(x => x.Key)
                            .First().Value;

                        weather = new WeatherSnapshotDto
                        {
                            TemperatureCelsius = firstForecast.TemperatureCelsius,
                            PrecipitationProbability = firstForecast.PrecipitationProbability,
                            WindSpeed = firstForecast.WindSpeed
                        };
                    }

                    stops.Add(new RouteStopDto
                    {
                        NodeId = nodeId,
                        Label = graphNode?.Label ?? nodeId,
                        DistanceFromPrevKm = edge?.DistanceKm ?? 0,
                        RouteType = edge?.RouteType ?? string.Empty,
                        Weather = weather
                    });
                }
                // ============================================
                // 7. Score route
                // ============================================

                var reasons = new List<SegmentReasonDetail>();

                double score = 100;

                var weatherStops = stops
                    .Where(x => x.Weather != null)
                    .ToList();

                var avgRain = weatherStops.Any()
                    ? weatherStops.Average(x => x.Weather!.PrecipitationProbability)
                    : 0;

                if (avgRain < 0.3)
                {
                    score += 10;
                    reasons.Add(new SegmentReasonDetail
                    {
                        Reason = SegmentReason.GoodWeather,
                        Metadata = new Dictionary<string, object>
                        {
                            ["avgRain"] = avgRain
                        }
                    });
                }
                else if (avgRain > 0.6)
                {
                    score -= 25;

                    reasons.Add(new SegmentReasonDetail
                    {
                        Reason = SegmentReason.AvoidRain,
                        Metadata = new Dictionary<string, object>
                        {
                            ["avgRain"] = avgRain
                        }
                    });
                }

                if (path.TotalDistanceKm < 700)
                {
                    score += 10;

                    reasons.Add(new SegmentReasonDetail
                    {
                        Reason = SegmentReason.ShortDistance
                    });
                }
                else
                {
                    score -= 10;

                    reasons.Add(new SegmentReasonDetail
                    {
                        Reason = SegmentReason.LongDistance
                    });
                }

                var scenicCount = stops.Count(x =>
                    x.RouteType.Contains("Đường Thủy") ||
                    x.RouteType.Contains("Tuyến đường khác"));

                if (scenicCount > 0)
                {
                    score += scenicCount * 3;

                    reasons.Add(new SegmentReasonDetail
                    {
                        Reason = SegmentReason.ScenicRoute,
                        Metadata = new Dictionary<string, object>
                        {
                            ["scenicSegments"] = scenicCount
                        }
                    });
                }
                // ============================================
                // 8. AI recommendation
                // ============================================

                var aiRecommendation = await GetAiRecommendationAsync(
                    startLocation.LocationName,
                    endLocation.LocationName,
                    trip.StartDate,
                    stops,
                    path.TotalDistanceKm,
                    score,
                    reasons);

                responses.Add(new RouteSuggestionResponse
                {
                    RouteId = BuildRouteId(path.Nodes),
                    RouteIndex = responses.Count + 1,
                    Stops = stops,
                    TotalDistanceKm = path.TotalDistanceKm,
                    WeatherSummary = BuildWeatherSummary(stops),
                    AiRecommendation = aiRecommendation,
                    Score = score,
                    Reasons = reasons
                });
            }

            // ============================================
            // 9. Mark best route
            // ============================================

            var best = responses
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (best != null)
                best.Recommended = true;

            return responses
                .OrderByDescending(x => x.Score)
                .ToList();
        }

        public async Task ApplyRouteAsync(Guid tripId, string routeId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ============================================
                // 1. Load route suggestions
                // ============================================

                var suggestions = await GetRouteSuggestionsAsync(tripId);

                var selected = suggestions
                    .FirstOrDefault(x => x.RouteId == routeId);

                if (selected == null)
                    throw new Exception("Route not found.");

                // ============================================
                // 2. Load segments
                // ============================================

                var segments = (await _segmentRepo
                    .GetByTripIdAsync(tripId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                if (segments.Count < 2)
                    throw new Exception("Trip must contain start/end segments.");

                var startSegment = segments.First();
                var endSegment = segments.Last();

                // ============================================
                // 3. Build new middle segments
                // ============================================

                var middleStops = selected.Stops
                    .Skip(1)
                    .SkipLast(1)
                    .Where(x =>
                    {
                        var node = _routeGraph.Nodes[x.NodeId];

                        return node.Type.Contains("Tỉnh") ||
                               node.Type.Contains("Thành phố");
                    })
                    .ToList();

                var trip = await _tripRepo.GetByIdAsync(tripId)
                    ?? throw new Exception("Trip not found");

                var totalDays =
                    (trip.EndDate.Date - trip.StartDate.Date).Days;

                var daysPerSegment = middleStops.Any()
                    ? Math.Max(1, totalDays / (middleStops.Count + 1))
                    : totalDays;

                var currentDate = trip.StartDate.Date;

                var newSegments = new List<TripSegment>();

                for (int i = 0; i < middleStops.Count; i++)
                {
                    var stop = middleStops[i];

                    var location = await _locationRepo
                        .GetByNameAsync(stop.Label);

                    if (location == null)
                        continue;

                    currentDate = currentDate.AddDays(daysPerSegment);

                    newSegments.Add(new TripSegment
                    {
                        SegmentId = Guid.NewGuid(),
                        TripId = tripId,
                        LocationId = location.LocationId,
                        DistrictId = null,
                        OrderIndex = i + 2,
                        StartDate = currentDate,
                        EndDate = currentDate,
                        CreatedAt = DateTime.UtcNow
                    }); 
                }

                await _segmentRepo.AddRangeAsync(newSegments);

                // ============================================
                // 4. Reorder end segment
                // ============================================

                endSegment.OrderIndex = newSegments.Count + 2;

                // ============================================
                // 5. Save changes
                // ============================================

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────
        // Helpers for route suggestions
        // ─────────────────────────────────────────────

        private static string BuildRouteId(List<string> nodes)
        {
            var raw = string.Join(">", nodes);

            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(raw));

            return Convert.ToBase64String(bytes);
        }

        private static string BuildWeatherSummary(List<RouteStopDto> stops)
        {
            var withWeather = stops.Where(s => s.Weather != null).ToList();
            if (!withWeather.Any()) return "Không có dữ liệu thời tiết.";

            var avgTemp  = withWeather.Average(s => s.Weather!.TemperatureCelsius);
            var avgRain  = withWeather.Average(s => s.Weather!.PrecipitationProbability);
            var avgWind  = withWeather.Average(s => s.Weather!.WindSpeed);

            var rainLabel = avgRain switch
            {
                < 0.3  => "ít mưa",
                < 0.6  => "khả năng mưa vừa",
                _      => "mưa nhiều"
            };

            return $"Nhiệt độ trung bình {avgTemp:F1}°C, {rainLabel} ({avgRain * 100:F0}%), " +
                   $"gió {avgWind:F1} km/h.";
        }

        private async Task<string> GetAiRecommendationAsync(
    string startLabel,
    string endLabel,
    DateTime tripStartDate,
    List<RouteStopDto> stops,
    double totalKm,
    double score,
    List<SegmentReasonDetail> reasons)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Bạn là chuyên gia du lịch Việt Nam.");

            sb.AppendLine(
                $"Tuyến đường từ {startLabel} đến {endLabel}. " +
                $"Khởi hành {tripStartDate:dd/MM/yyyy}. " +
                $"Tổng quãng đường {totalKm:F0} km.");

            sb.AppendLine($"Điểm đánh giá hệ thống: {score:F0}/100");

            sb.AppendLine("Lý do đánh giá:");

            foreach (var reason in reasons)
            {
                sb.AppendLine($"- {reason.Reason}");
            }

            sb.AppendLine();

            sb.AppendLine("Các điểm dừng:");

            foreach (var stop in stops)
            {
                var weather = stop.Weather != null
                    ? $"nhiệt độ {stop.Weather.TemperatureCelsius:F1}°C, " +
                      $"mưa {stop.Weather.PrecipitationProbability * 100:F0}%"
                    : "không có dữ liệu thời tiết";

                sb.AppendLine(
                    $"- {stop.Label}: {weather}");
            }

            sb.AppendLine();
            sb.AppendLine("""
                Đánh giá tuyến đường theo:
                - thời tiết
                - độ dài
                - trải nghiệm du lịch
                - tính thuận tiện

                Nếu tuyến này tốt hơn đa số tuyến khác, hãy nói rõ vì sao.

                Trả JSON:
                {
                  "summary": "...",
                  "recommended": true/false,
                  "highlights": ["...", "..."]
                }
                """);

            try
            {
                var raw     = await _gemini.GenerateAsync(sb.ToString());
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement
                    .GetProperty("recommendation")
                    .GetString() ?? raw;
            }
            catch
            {
                // Gemini unavailable — return weather summary as fallback
                return BuildWeatherSummary(stops);
            }
        }

        private async Task RecalculateDistances(List<TripSegment> segments)
        {
            var ordered = segments
                .OrderBy(x => x.OrderIndex)
                .ToList();

            if (!ordered.Any()) return;

            // 🔥 first segment = 0
            ordered[0].DistanceKm = 0;

            var locationIds = ordered
                .Select(x => x.LocationId)
                .Distinct()
                .ToList();

            var locationDict = await _locationRepo
                .GetByIdsAsDictionaryAsync(locationIds);

            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];

                var prevLoc = locationDict[prev.LocationId];
                var currLoc = locationDict[curr.LocationId];

                curr.DistanceKm = await _geocodingService.GetDrivingDistance(
                    prevLoc.Latitude, prevLoc.Longitude,
                    currLoc.Latitude, currLoc.Longitude
                );
            }
        }
    }
}

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
                if (segment.DistrictId.HasValue)
                {
                    var district = await _districtRepo.GetByIdAsync(segment.DistrictId);

                    if (district == null)
                        throw new Exception($"District {segment.DistrictId} not found");

                    if (district.LocationId != segment.LocationId)
                        throw new Exception($"District does not belong to location");
                }

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

            // ============================================
            // 8. Save new segments FIRST
            // ============================================
            await _segmentRepo.AddRangeAsync(newSegments);

            await _unitOfWork.SaveChangesAsync();

            // ============================================
            // 9. Reload ALL segments
            // ============================================
            var allSegments = (await _segmentRepo.GetByTripIdAsync(tripId))
                .OrderBy(x => x.OrderIndex)
                .ToList();

            // ============================================
            // 10. Normalize order
            // ============================================
            for(int i = 0; i < allSegments.Count; i++)
            {
                allSegments[i].OrderIndex = i + 1;
            }

            // ============================================
            // 11. Recalculate distances
            // ============================================
            await RecalculateDistances(allSegments);

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

        public async Task<List<RouteOptionDTO>>
    GetAvailableRoutesAsync(Guid tripId)
        {
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

            var paths = _routeGraph.FindTopKPaths(
                startLocation.LocationName,
                endLocation.LocationName,
                5);

            var allLocations = await _locationRepo.GetAllAsync();

            var locationByName = allLocations
                .GroupBy(
                    x => x.LocationName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

            return paths
                .Select((path, index) =>
                {
                    var polyline = new List<RoutePolylinePointDto>();

                    foreach (var nodeId in path.Nodes)
                    {
                        if (!_routeGraph.Nodes.TryGetValue(nodeId, out var graphNode))
                            continue;

                        Location? dbLoc = null;

                        if (locationByName.TryGetValue(graphNode.Label, out var byLabel))
                            dbLoc = byLabel;
                        else if (locationByName.TryGetValue(graphNode.Id, out var byId))
                            dbLoc = byId;

                        if (dbLoc == null)
                            continue;

                        polyline.Add(new RoutePolylinePointDto
                        {
                            Latitude = dbLoc.Latitude,
                            Longitude = dbLoc.Longitude
                        });
                    }

                    return new RouteOptionDTO
                    {
                        RouteId = BuildRouteId(path.Nodes),
                        RouteIndex = index + 1,
                        TotalDistanceKm = path.TotalDistanceKm,
                        Nodes = path.Nodes,
                        Polyline = polyline
                    };
                })
                .ToList(); ;
        }

        //    public async Task<List<RouteSuggestionResponse>>
        //GetRouteSuggestionsAsync(Guid tripId)
        //    {
        //        // ============================================
        //        // 1. Load trip segments
        //        // ============================================

        //        var segments = (await _segmentRepo
        //            .GetByTripIdAsync(tripId))
        //            .OrderBy(x => x.OrderIndex)
        //            .ToList();

        //        if (segments.Count < 2)
        //            throw new Exception("Trip must contain start/end segments.");

        //        var startSegment = segments.First();
        //        var endSegment = segments.Last();

        //        var startLocation = await _locationRepo
        //            .GetByIdAsync(startSegment.LocationId);

        //        var endLocation = await _locationRepo
        //            .GetByIdAsync(endSegment.LocationId);

        //        if (startLocation == null || endLocation == null)
        //            throw new Exception("Start/end location not found");

        //        // check graph mapping using location name
        //        if (!_routeGraph.NodeExists(startLocation.LocationName) ||
        //            !_routeGraph.NodeExists(endLocation.LocationName))
        //        {
        //            throw new Exception("Graph node mapping missing");
        //        }

        //        // ============================================
        //        // 2. Find graph routes
        //        // ============================================

        //        var paths = _routeGraph.FindTopKPaths(
        //            startLocation.LocationName,
        //            endLocation.LocationName,
        //            5);

        //        if (!paths.Any())
        //            return new List<RouteSuggestionResponse>();
        //        // ============================================
        //        // 3. Preload locations
        //        // ============================================

        //        var allLocations = await _locationRepo.GetAllAsync();

        //        var locationByName = allLocations
        //            .GroupBy(x => x.LocationName.Trim(),
        //                StringComparer.OrdinalIgnoreCase)
        //            .ToDictionary(
        //                x => x.Key,
        //                x => x.First(),
        //                StringComparer.OrdinalIgnoreCase);

        //        // ============================================
        //        // 4. Trip dates
        //        // ============================================

        //        var trip = await _tripRepo.GetByIdAsync(tripId)
        //            ?? throw new Exception("Trip not found");

        //        var tripDates = Enumerable
        //            .Range(0,
        //                Math.Max(1,
        //                    (trip.EndDate.Date - trip.StartDate.Date).Days + 1))
        //            //.Select(i => trip.StartDate.Date.AddDays(i))
        //            .Select(i =>
        //DateTime.SpecifyKind(
        //    trip.StartDate.Date.AddDays(i),
        //    DateTimeKind.Utc))
        //            .ToList();
        //        // ============================================
        //        // 5. Preload weather
        //        // ============================================

        //        var uniqueNodeIds = paths
        //            .SelectMany(x => x.Nodes)
        //            .Distinct(StringComparer.OrdinalIgnoreCase)
        //    .ToList();

        //        var nodeToLocation =
        //            new Dictionary<string, Location?>(
        //                StringComparer.OrdinalIgnoreCase);

        //        foreach (var nodeId in uniqueNodeIds)
        //        {
        //            var graphNode = _routeGraph.Nodes
        //                .TryGetValue(nodeId, out var gn)
        //                    ? gn
        //                    : null;

        //            if (graphNode == null)
        //            {
        //                nodeToLocation[nodeId] = null;
        //                continue;
        //            }

        //            Location? dbLoc = null;

        //            if (locationByName.TryGetValue(graphNode.Label, out var byLabel))
        //                dbLoc = byLabel;
        //            else if (locationByName.TryGetValue(graphNode.Id, out var byId))
        //                dbLoc = byId;

        //            nodeToLocation[nodeId] = dbLoc;
        //        }

        //        var weatherByLocationId =
        //            new Dictionary<Guid, Dictionary<DateTime, WeatherForecast>>();

        //        var validLocations = nodeToLocation
        //            .Values
        //            .Where(x => x != null)
        //            .DistinctBy(x => x!.LocationId)
        //            .Select(x => x!)
        //            .ToList();

        //        /*var weatherTasks = validLocations.Select(async loc =>
        //        //{
        //        //    var forecasts = await _weatherService
        //        //        .GetRangeOptimizedAsync(loc.LocationId, tripDates);

        //        //    return (loc.LocationId, forecasts);
        //        //});

        //        //var weatherResults = await Task.WhenAll(weatherTasks);

        //        //foreach (var result in weatherResults)
        //        //{
        //        //    weatherByLocationId[result.LocationId] = result.forecasts;
        //        }
        //    */

        //        foreach (var loc in validLocations)
        //        {
        //            var forecasts = await _weatherService
        //                .GetRangeOptimizedAsync(loc.LocationId, tripDates);

        //            weatherByLocationId[loc.LocationId] = forecasts;
        //        }

        //        // ============================================
        //        // 6. Build responses
        //        // ============================================

        //        var responses = new List<RouteSuggestionResponse>();

        //        foreach (var path in paths)
        //        {
        //            var stops = new List<RouteStopDto>();

        //            var edgeMap = new Dictionary<string, GraphEdge>();

        //            foreach (var edge in path.Edges)
        //                edgeMap[edge.Target] = edge;

        //            foreach (var nodeId in path.Nodes)
        //            {
        //                var graphNode = _routeGraph.Nodes
        //                    .TryGetValue(nodeId, out var gn)
        //                        ? gn
        //                        : null;

        //                var edge = edgeMap.TryGetValue(nodeId, out var e)
        //                    ? e
        //                    : null;

        //                var dbLoc = nodeToLocation.TryGetValue(nodeId, out var loc)
        //                    ? loc
        //                    : null;

        //                WeatherSnapshotDto? weather = null;

        //                if (dbLoc != null &&
        //                    weatherByLocationId.TryGetValue(dbLoc.LocationId, out var forecasts) &&
        //                    forecasts.Any())
        //                {
        //                    var firstForecast = forecasts
        //                        .OrderBy(x => x.Key)
        //                        .First().Value;

        //                    weather = new WeatherSnapshotDto
        //                    {
        //                        TemperatureCelsius = firstForecast.TemperatureCelsius,
        //                        PrecipitationProbability = firstForecast.PrecipitationProbability,
        //                        WindSpeed = firstForecast.WindSpeed
        //                    };
        //                }

        //                stops.Add(new RouteStopDto
        //                {
        //                    NodeId = nodeId,
        //                    Label = graphNode?.Label ?? nodeId,
        //                    DistanceFromPrevKm = edge?.DistanceKm ?? 0,
        //                    RouteType = edge?.RouteType ?? string.Empty,
        //                    Weather = weather
        //                });
        //            }
        //            // ============================================
        //            // 7. Weather analysis
        //            // ============================================

        //            var weatherStops = stops
        //.Where(x => x.Weather != null)
        //.ToList();

        //            var avgRain = weatherStops.Any()
        //                ? weatherStops.Average(x => x.Weather!.PrecipitationProbability)
        //                : 0;

        //            var warnings = new List<string>();

        //            if (avgRain >= 0.8)
        //            {
        //                warnings.Add(
        //                    "Khả năng mưa rất cao trên toàn tuyến. " +
        //                    "Cần chuẩn bị áo mưa và hạn chế hoạt động ngoài trời.");
        //            }
        //            else if (avgRain >= 0.5)
        //            {
        //                warnings.Add(
        //                    "Có khả năng mưa trong chuyến đi.");
        //            }

        //            if (path.TotalDistanceKm > 800)
        //            {
        //                warnings.Add(
        //                    "Tuyến đường khá dài, nên chuẩn bị thời gian nghỉ hợp lý.");
        //            }
        //            // ============================================
        //            // 8. AI recommendation
        //            // ============================================

        //            var aiRecommendation = await GetAiRecommendationAsync(
        //                startLocation.LocationName,
        //                endLocation.LocationName,
        //                trip.StartDate,
        //                stops,
        //                path.TotalDistanceKm);

        //            responses.Add(new RouteSuggestionResponse
        //            {
        //                RouteId = BuildRouteId(path.Nodes),
        //                RouteIndex = responses.Count + 1,
        //                Stops = stops,
        //                TotalDistanceKm = path.TotalDistanceKm,
        //                WeatherSummary = BuildWeatherSummary(stops),
        //                RecommendedActivities =
        //                    aiRecommendation?.RecommendedActivities ?? new(),
        //                Warnings =
        //                    aiRecommendation?.Warnings ?? new()
        //            });
        //        }

        //        // ============================================
        //        // 9. Mark best route
        //        // ============================================

        //        var best = responses
        //            .OrderByDescending(x => x.Score)
        //            .FirstOrDefault();

        //        if (best != null)
        //            best.Recommended = true;

        //        return responses
        //            .OrderByDescending(x => x.Score)
        //            .ToList();
        //    }

        public async Task<RouteSuggestionResponse>
    GetRouteSuggestionAsync(
        Guid tripId,
        string routeId)
        {
            // ============================================
            // 1. Get all available routes
            // ============================================

            var routes = await GetAvailableRoutesAsync(tripId);

            var selectedRoute = routes
                .FirstOrDefault(x => x.RouteId == routeId);

            if (selectedRoute == null)
                throw new Exception("Route not found.");

            // ============================================
            // 2. Load trip
            // ============================================

            var trip = await _tripRepo.GetByIdAsync(tripId)
                ?? throw new Exception("Trip not found.");

            // ============================================
            // 3. Load locations
            // ============================================

            var allLocations = await _locationRepo.GetAllAsync();

            var locationByName = allLocations
                .GroupBy(
                    x => x.LocationName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

            // ============================================
            // 4. Trip dates
            // ============================================

            var tripDates = Enumerable
                .Range(
                    0,
                    Math.Max(
                        1,
                        (trip.EndDate.Date - trip.StartDate.Date).Days + 1))
                .Select(i =>
                    DateTime.SpecifyKind(
                        trip.StartDate.Date.AddDays(i),
                        DateTimeKind.Utc))
                .ToList();

            // ============================================
            // 5. Build stops
            // ============================================

            var stops = new List<RouteStopDto>();

            for (int i = 0; i < selectedRoute.Nodes.Count; i++)
            {
                var nodeId = selectedRoute.Nodes[i];
                if (!_routeGraph.Nodes.TryGetValue(nodeId, out var graphNode))
                    continue;

                Location? dbLoc = null;

                if (locationByName.TryGetValue(graphNode.Label, out var byLabel))
                    dbLoc = byLabel;
                else if (locationByName.TryGetValue(graphNode.Id, out var byId))
                    dbLoc = byId;

                WeatherSnapshotDto? weather = null;

                // ========================================
                // Load weather
                // ========================================

                if (dbLoc != null)
                {
                    var forecasts = await _weatherService
                        .GetRangeOptimizedAsync(
                            dbLoc.LocationId,
                            tripDates);

                    var firstForecast = forecasts
                        .OrderBy(x => x.Key)
                        .FirstOrDefault()
                        .Value;

                    if (firstForecast != null)
                    {
                        weather = new WeatherSnapshotDto
                        {
                            TemperatureCelsius =
                                firstForecast.TemperatureCelsius,

                            PrecipitationProbability =
                                firstForecast.PrecipitationProbability,

                            WindSpeed =
                                firstForecast.WindSpeed
                        };
                    }
                }

                // ========================================
                // Edge info
                // ========================================

                double distanceKm = 0;
                string routeType = "";

                // lấy edge từ node trước -> node hiện tại
                if (i > 0)
                {
                    var prevNodeId = selectedRoute.Nodes[i - 1];

                    var edge = selectedRoute.Edges
                        .FirstOrDefault(x =>
                            x.Source.Equals(
                                prevNodeId,
                                StringComparison.OrdinalIgnoreCase)
                            &&
                            x.Target.Equals(
                                nodeId,
                                StringComparison.OrdinalIgnoreCase));

                    if (edge != null)
                    {
                        distanceKm = edge.DistanceKm;
                        routeType = edge.RouteType;
                    }
                }

                stops.Add(new RouteStopDto
                {
                    NodeId = nodeId,

                    Label = graphNode.Label,

                    Latitude = dbLoc?.Latitude,

                    Longitude = dbLoc?.Longitude,

                    DistanceFromPrevKm = distanceKm,

                    RouteType = routeType,

                    Weather = weather
                });
            }

            // ============================================
            // 6. Weather analysis
            // ============================================

            var weatherStops = stops
                .Where(x => x.Weather != null)
                .ToList();

            var avgRain = weatherStops.Any()
                ? weatherStops.Average(
                    x => x.Weather!.PrecipitationProbability)
                : 0;

            var warnings = new List<string>();

            if (avgRain >= 0.8)
            {
                warnings.Add(
                    "Khả năng mưa rất cao trên toàn tuyến.");
            }
            else if (avgRain >= 0.5)
            {
                warnings.Add(
                    "Có khả năng mưa trong chuyến đi.");
            }

            if (selectedRoute.TotalDistanceKm > 800)
            {
                warnings.Add(
                    "Tuyến đường khá dài, nên nghỉ giữa chặng.");
            }

            // ============================================
            // 7. AI recommendation
            // ============================================

            var travelAdvice =
                await GetAiRecommendationAsync(
                    stops.FirstOrDefault()?.Label ?? "",
                    stops.LastOrDefault()?.Label ?? "",
                    trip.StartDate,
                    stops,
                    selectedRoute.TotalDistanceKm);

            // ============================================
            // 8. Return response
            // ============================================

            return new RouteSuggestionResponse
            {
                RouteId = selectedRoute.RouteId,

                RouteIndex = selectedRoute.RouteIndex,

                Stops = stops,

                TotalDistanceKm =
                    selectedRoute.TotalDistanceKm,

                WeatherSummary =
                    BuildWeatherSummary(stops),

                TravelAdvice = travelAdvice,

                Warnings = warnings,

                Polyline = selectedRoute.Polyline
            };
        }

        public async Task ApplyRouteAsync(
    Guid tripId,
    RouteOptionDTO selectedRoute)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ============================================
                // 1. Validate trip
                // ============================================

                var trip = await _tripRepo.GetByIdAsync(tripId)
                    ?? throw new Exception("Trip not found");

                var segments = trip.TripSegments
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                if (segments.Count < 2)
                    throw new Exception(
                        "Trip must contain start/end segments");

                // ============================================
                // 2. Remove old middle segments
                // ============================================

                var oldMiddleSegments = segments
                    .Skip(1)
                    .SkipLast(1)
                    .ToList();

                if (oldMiddleSegments.Any())
                {
                    await _segmentRepo.DeleteByIdsAsync(
                        oldMiddleSegments
                            .Select(x => x.SegmentId)
                            .ToList());
                }

                // ============================================
                // 3. Reload segments after delete
                // ============================================

                segments = (await _segmentRepo
                    .GetByTripIdAsync(tripId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                var startSegment = segments.First();
                var endSegment = segments.Last();

                // ============================================
                // 4. Extract middle nodes from route
                // ============================================

                var middleNodeIds = selectedRoute.Nodes
                    .Skip(1)
                    .SkipLast(1)
                    .ToList();

                // no middle stops
                if (!middleNodeIds.Any())
                {
                    await RecalculateDistances(segments);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();

                    return;
                }

                // ============================================
                // 5. Resolve graph nodes -> locations
                // ============================================

                var allLocations = await _locationRepo
                    .GetAllAsync();

                var locationByName = allLocations
                    .GroupBy(
                        x => x.LocationName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

                // ============================================
                // 6. Build new segments
                // ============================================

                var tripDays = Math.Max(
                    1,
                    (trip.EndDate.Date - trip.StartDate.Date).Days);

                var daysPerStop = Math.Max(
                    1,
                    tripDays / (middleNodeIds.Count + 1));

                var currentDate = trip.StartDate.Date;

                var newSegments = new List<TripSegment>();

                foreach (var nodeId in middleNodeIds)
                {
                    // graph node
                    if (!_routeGraph.Nodes.TryGetValue(
                        nodeId,
                        out var graphNode))
                    {
                        continue;
                    }

                    // location
                    if (!locationByName.TryGetValue(
                        graphNode.Label,
                        out var location))
                    {
                        continue;
                    }

                    currentDate =
                        currentDate.AddDays(daysPerStop);

                    newSegments.Add(new TripSegment
                    {
                        SegmentId = Guid.NewGuid(),

                        TripId = tripId,

                        LocationId = location.LocationId,

                        DistrictId = null,

                        OrderIndex = 0, // InsertSegmentsAsync handles this

                        StartDate = currentDate,

                        EndDate = currentDate,

                        CreatedAt = DateTime.UtcNow
                    });
                }

                // ============================================
                // 7. Insert new middle segments
                // ============================================

                if (newSegments.Any())
                {
                    await InsertSegmentsAsync(
                        tripId,
                        2,
                        newSegments);
                }

                // ============================================
                // 8. Final reload + distance recalc
                // ============================================

                var finalSegments = (await _segmentRepo
                    .GetByTripIdAsync(tripId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                await RecalculateDistances(finalSegments);

                // ============================================
                // 9. Save
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


        public async Task UpdateSegmentAsync(
    Guid tripId,
    Guid segmentId,
    TripSegment updatedSegment)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // ============================================
                // 1. Validate trip
                // ============================================

                var trip = await _tripRepo.GetByIdAsync(tripId);

                if (trip == null)
                    throw new Exception("Trip not found");

                // ============================================
                // 2. Find segment
                // ============================================

                var segments = await _segmentRepo
                    .GetByTripIdAsync(tripId);

                var existingSegment = segments
                    .FirstOrDefault(x => x.SegmentId == segmentId);

                if (existingSegment == null)
                    throw new Exception("Segment not found");

                // ============================================
                // 3. Validate location
                // ============================================

                var location = await _locationRepo
                    .GetByIdAsync(updatedSegment.LocationId);

                if (location == null)
                    throw new Exception("Location not found");

                // ============================================
                // 4. Validate district (optional)
                // ============================================

                if (updatedSegment.DistrictId.HasValue)
                {
                    var district = await _districtRepo
                        .GetByIdAsync(updatedSegment.DistrictId.Value);

                    if (district == null)
                        throw new Exception("District not found");

                    if (district.LocationId != updatedSegment.LocationId)
                        throw new Exception(
                            "District does not belong to location");
                }

                // ============================================
                // 5. Update fields
                // ============================================

                existingSegment.LocationId =
                    updatedSegment.LocationId;

                existingSegment.DistrictId =
                    updatedSegment.DistrictId;

                existingSegment.StartDate =
                    updatedSegment.StartDate;

                existingSegment.EndDate =
                    updatedSegment.EndDate;

                // ============================================
                // 6. Recalculate distances
                // ============================================

                var orderedSegments = segments
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                await RecalculateDistances(orderedSegments);

                // ============================================
                // 7. Save
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

        //─────────────────────────────────────────────
        //Helpers for route suggestions
        //─────────────────────────────────────────────

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
    double totalKm)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Bạn là chuyên gia du lịch Việt Nam.");

            sb.AppendLine(
                $"Tuyến đường từ {startLabel} đến {endLabel}. " +
                $"Khởi hành {tripStartDate:dd/MM/yyyy}. " +
                $"Tổng quãng đường {totalKm:F0} km.");

            sb.AppendLine("Lý do đánh giá:");

            sb.AppendLine();

            sb.AppendLine("Các điểm dừng:");

            foreach (var stop in stops)
            {
                string rainLabel =
                    stop.Weather!.PrecipitationProbability switch
                    {
                        < 0.3 => "ít mưa",
                        < 0.6 => "có thể có mưa",
                        _ => "mưa lớn"
                    };

                var weather = stop.Weather != null
                    ? $"nhiệt độ {stop.Weather.TemperatureCelsius:F1}°C, " +
                     $"{rainLabel}"+
                      $"mưa {stop.Weather.PrecipitationProbability * 100:F0}%"
                    : "không có dữ liệu thời tiết";
                var rain = stop.Weather?.PrecipitationProbability ?? 0;

                var activityHint =
                    rain > 0.6
                        ? "ưu tiên hoạt động trong nhà"
                        : "phù hợp hoạt động ngoài trời";

                sb.AppendLine(
                    $"- {stop.Label}: {weather}, {activityHint}");
            }

            sb.AppendLine();
            sb.AppendLine("""
                Hãy phân tích tuyến du lịch này theo:
                - thời tiết
                - trải nghiệm du lịch
                - mức độ thuận tiện di chuyển

                Nếu khả năng mưa cao:
                - ưu tiên gợi ý hoạt động indoor
                - hạn chế hoạt động ngoài trời
                - cảnh báo rủi ro khi di chuyển

                Nếu thời tiết đẹp:
                - gợi ý hoạt động outdoor phù hợp.

                Trả lời cực ngắn gọn bằng tiếng Việt.
                Tối đa 2 câu.
                Tập trung:
                - thời tiết
                - độ thuận tiện di chuyển
                - nên indoor hay outdoor

                Trả JSON:
                {
                  "summary": "...",
                  "recommended": true/false,
                  "highlights": ["...", "..."]
                }
                """);

            try
            {
                var raw = await _gemini.GenerateAsync(sb.ToString());

                using var doc = JsonDocument.Parse(raw);

                var summary = doc.RootElement
                    .GetProperty("summary")
                    .GetString();

                var highlights = "";

                if (doc.RootElement.TryGetProperty("highlights", out var hl))
                {
                    highlights = string.Join(
                        ", ",
                        hl.EnumerateArray()
                          .Select(x => x.GetString()));
                }

                return $"{summary} Điểm nổi bật: {highlights}";
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

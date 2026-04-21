using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Weather;

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

        public TripSegmentService(ITripSegmentRepository segmentRepo, ILocationRepository locationRepository, IWeatherForecastRepository weatherForecastRepository, IAdaptiveWeatherRiskEngine adaptiveWeatherRiskEngine, ITripRepository tripRepo, IGeocodingService geocodingService, IUnitOfWork unitOfWork)
        {
            _segmentRepo = segmentRepo;
            _locationRepo = locationRepository;
            _weatherRepo = weatherForecastRepository;
            _riskEngine = adaptiveWeatherRiskEngine;
            _tripRepo = tripRepo;
            _geocodingService = geocodingService;
            _unitOfWork = unitOfWork;
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

        public async Task<List<TripSegment>> AddSegmentsToTripAsync(
    Guid tripId,
    List<TripSegment> segments)
        {
            // 1. Check Trip
            var trip = await _tripRepo.GetByIdAsync(tripId);
            if (trip == null)
                throw new Exception("Trip not found");

            if (segments == null || !segments.Any())
                throw new Exception("Segments cannot be empty");

            // 2. Sort input
            segments = segments.OrderBy(x => x.OrderIndex).ToList();

            // 3. Auto add return segment (RoundTrip)
            if (trip.TripType == TripType.RoundTrip)
            {
                var first = segments.First();
                var last = segments.Last();

                if (first.LocationId != last.LocationId)
                {
                    segments.Add(new TripSegment
                    {
                        LocationId = first.LocationId,
                        StartDate = last.EndDate.AddDays(1),
                        EndDate = last.EndDate.AddDays(1),
                        OrderIndex = last.OrderIndex + 1
                    });
                }
            }

            // 4. Get existing segments
            var existingSegments = trip.TripSegments ?? new List<TripSegment>();
            int currentMaxOrder = existingSegments.Any()
                ? existingSegments.Max(x => x.OrderIndex)
                : 0;

            // 5. Preload ALL locations (new + previous)
            var locationIds = segments.Select(x => x.LocationId).ToList();

            TripSegment? prevSegment = existingSegments
                .OrderBy(x => x.OrderIndex)
                .LastOrDefault();

            if (prevSegment != null)
                locationIds.Add(prevSegment.LocationId);

            locationIds = locationIds.Distinct().ToList();

            var locationDict = await _locationRepo.GetByIdsAsDictionaryAsync(locationIds);

            // 6. Resolve previous location
            Location? prevLocation = null;

            if (prevSegment != null)
            {
                if (!locationDict.ContainsKey(prevSegment.LocationId))
                    throw new Exception("Previous location not found");

                prevLocation = locationDict[prevSegment.LocationId];
            }

            // 7. Assign + calculate distance
            int index = 1;

            foreach (var segment in segments)
            {
                if (!locationDict.ContainsKey(segment.LocationId))
                    throw new Exception($"Location {segment.LocationId} not found");

                var currentLocation = locationDict[segment.LocationId];

                segment.SegmentId = Guid.NewGuid();
                segment.TripId = tripId;
                segment.CreatedAt = DateTime.UtcNow;
                segment.OrderIndex = currentMaxOrder + index;

                // 🔥 Distance logic
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

            // 8. Save
            await _segmentRepo.AddRangeAsync(segments);

            return segments;
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
            if (nextSegment != null && prevLocation != null)
            {
                if (!locationDict.ContainsKey(nextSegment.LocationId))
                    throw new Exception("Next location not found");

                var nextLocation = locationDict[nextSegment.LocationId];

                nextSegment.DistanceKm = await _geocodingService.GetDrivingDistance(
                    prevLocation.Latitude, prevLocation.Longitude,
                    nextLocation.Latitude, nextLocation.Longitude
                );
            }

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
                for (int i = 0; i <= remaining.Count; i++)
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
    }
}

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

        public TripSegmentService(ITripSegmentRepository segmentRepo, ILocationRepository locationRepository, IWeatherForecastRepository weatherForecastRepository, IAdaptiveWeatherRiskEngine adaptiveWeatherRiskEngine, ITripRepository tripRepo, IGeocodingService geocodingService)
        {
            _segmentRepo = segmentRepo;
            _locationRepo = locationRepository;
            _weatherRepo = weatherForecastRepository;
            _riskEngine = adaptiveWeatherRiskEngine;
            _tripRepo = tripRepo;
            _geocodingService = geocodingService;
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
            {
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

                // 🔥 VALIDATE POSITION
                if (insertAt < 1 || insertAt > existing.Count)
                    throw new Exception("Invalid insert position");

                // 🔥 SHIFT EXISTING SEGMENTS
                int shift = newSegments.Count;

                foreach (var seg in existing.Where(x => x.OrderIndex >= insertAt))
                {
                    seg.OrderIndex += shift;
                }

                // 🔥 PRELOAD LOCATIONS
                var locationIds = newSegments.Select(x => x.LocationId).ToList();

                var prevSegment = existing
                    .FirstOrDefault(x => x.OrderIndex == insertAt - 1);

                var nextSegment = existing
                    .FirstOrDefault(x => x.OrderIndex == insertAt + shift);

                if (prevSegment != null) locationIds.Add(prevSegment.LocationId);
                if (nextSegment != null) locationIds.Add(nextSegment.LocationId);

                locationIds = locationIds.Distinct().ToList();

                var locationDict = await _locationRepo
                    .GetByIdsAsDictionaryAsync(locationIds);

                // 🔥 RESOLVE PREVIOUS LOCATION
                Location? prevLocation = null;

                if (prevSegment != null)
                    prevLocation = locationDict[prevSegment.LocationId];

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

                    // 🔥 DISTANCE CALCULATION
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

                // 🔥 FIX DISTANCE FOR NEXT SEGMENT (VERY IMPORTANT)
                if (nextSegment != null && prevLocation != null)
                {
                    var nextLocation = locationDict[nextSegment.LocationId];

                    nextSegment.DistanceKm = await _geocodingService.GetDrivingDistance(
                        prevLocation.Latitude, prevLocation.Longitude,
                        nextLocation.Latitude, nextLocation.Longitude
                    );
                }

                // 🔥 SAVE
                await _segmentRepo.AddRangeAsync(newSegments);

                return newSegments;
            }
        }

        public async Task<List<Location>> GetAllAsync()
        {
            var locations = await _locationRepo.GetAllAsync();
            return locations;
        }
    }
}

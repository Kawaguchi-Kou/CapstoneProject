using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
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

        public TripSegmentService(ITripSegmentRepository segmentRepo, ILocationRepository locationRepository, IWeatherForecastRepository weatherForecastRepository, IAdaptiveWeatherRiskEngine adaptiveWeatherRiskEngine, ITripRepository tripRepo)
        {
            _segmentRepo = segmentRepo;
            _locationRepo = locationRepository;
            _weatherRepo = weatherForecastRepository;
            _riskEngine = adaptiveWeatherRiskEngine;
            _tripRepo = tripRepo;
        }

        public async Task<List<Location>> RecommendSegmentsAsync(
            DateOnly startDate,
            DateOnly endDate,
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
            // 1. Check Trip exists
            var trip = await _tripRepo.GetByIdAsync(tripId);
            if (trip == null)
                throw new Exception("Trip not found");

            // 2. Get current max order
            var existingSegments = trip.TripSegments ?? new List<TripSegment>();
            int currentMaxOrder = existingSegments.Any()
                ? existingSegments.Max(x => x.OrderIndex)
                : 0;

            // 3. Assign data
            int index = 1;
            foreach (var segment in segments)
            {
                segment.SegmentId = Guid.NewGuid();
                segment.TripId = tripId;
                segment.CreatedAt = DateTime.UtcNow;

                // append order
                segment.OrderIndex = currentMaxOrder + index;
                index++;
            }

            // 4. Save
            await _segmentRepo.AddRangeAsync(segments);

            return segments;
        }
    }
}

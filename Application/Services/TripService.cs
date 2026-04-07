using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IBackgroundJobService _jobService;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripSegmentRepository _segmentRepo;

        public TripService(ITripRepository tripRepo, IBackgroundJobService jobService, ILocationRepository locationRepository, ITripSegmentRepository segmentRepo)
        {
            _tripRepo = tripRepo;
            _jobService = jobService;
            _locationRepository = locationRepository;
            _segmentRepo = segmentRepo;
        }


        public async Task<Trip> CreateTripAsync(Trip newTrip)
        {
            newTrip.Status = TripStatus.InProgress;
            newTrip.CreatedAt = DateTime.UtcNow;

            await _tripRepo.AddAsync(newTrip);

            // 🔥 AUTO CREATE BASE SEGMENTS
            var baseSegments = new List<TripSegment>();

            // 1. Start → End
            baseSegments.Add(new TripSegment
            {
                SegmentId = Guid.NewGuid(),
                TripId = newTrip.TripId,
                LocationId = _locationRepository.GetByNameAsync(newTrip.StartLocation).Result.LocationId,
                StartDate = newTrip.StartDate,
                EndDate = newTrip.StartDate,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow
            });

            baseSegments.Add(new TripSegment
            {
                SegmentId = Guid.NewGuid(),
                TripId = newTrip.TripId,
                LocationId = _locationRepository.GetByNameAsync(newTrip.EndLocation).Result.LocationId,
                StartDate = newTrip.EndDate,
                EndDate = newTrip.EndDate,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow
            });

            // 2. RoundTrip → add return segment
            if (newTrip.TripType == TripType.RoundTrip)
            {
                baseSegments.Add(new TripSegment
                {
                    SegmentId = Guid.NewGuid(),
                    TripId = newTrip.TripId,
                    LocationId = _locationRepository.GetByNameAsync(newTrip.StartLocation).Result.LocationId,
                    StartDate = newTrip.EndDate.AddDays(1),
                    EndDate = newTrip.EndDate.AddDays(1),
                    OrderIndex = 3,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _segmentRepo.AddRangeAsync(baseSegments);

            _jobService.EnqueueWeatherPreload(newTrip.TripId);

            return newTrip;
        }
    }
}

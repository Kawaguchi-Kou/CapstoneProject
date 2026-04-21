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
        private readonly IUnitOfWork _unitOfWork;

        public TripService(ITripRepository tripRepo, IBackgroundJobService jobService, ILocationRepository locationRepository, ITripSegmentRepository segmentRepo, IUnitOfWork unitOfWork)
        {
            _tripRepo = tripRepo;
            _jobService = jobService;
            _locationRepository = locationRepository;
            _segmentRepo = segmentRepo;
            _unitOfWork = unitOfWork;
        }


        public async Task<Trip> CreateTripAsync(Trip newTrip)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                newTrip.Status = TripStatus.InProgress;
                newTrip.CreatedAt = DateTime.UtcNow;

                await _tripRepo.AddAsync(newTrip);

                var locationNames = new List<string>
                {
                    newTrip.StartLocation,
                    newTrip.EndLocation
                };

                var locations = await _locationRepository.GetByNamesAsync(locationNames);
                var locationDict = locations.ToDictionary(x => x.LocationName);

                if (!locationDict.ContainsKey(newTrip.StartLocation) ||
                    !locationDict.ContainsKey(newTrip.EndLocation))
                    throw new Exception("Invalid start or end location");

                var startLocationId = locationDict[newTrip.StartLocation].LocationId;
                var endLocationId = locationDict[newTrip.EndLocation].LocationId;

                // 🔥 AUTO CREATE BASE SEGMENTS
                var baseSegments = new List<TripSegment>();

                // 1. Start → End
                baseSegments.Add(new TripSegment
                {
                    SegmentId = Guid.NewGuid(),
                    TripId = newTrip.TripId,
                    LocationId = startLocationId,
                    StartDate = newTrip.StartDate,
                    EndDate = newTrip.StartDate,
                    OrderIndex = 1,
                    CreatedAt = DateTime.UtcNow
                });

                baseSegments.Add(new TripSegment
                {
                    SegmentId = Guid.NewGuid(),
                    TripId = newTrip.TripId,
                    LocationId = endLocationId,
                    StartDate = newTrip.StartDate.AddDays(1),
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
                        LocationId = startLocationId,
                        StartDate = newTrip.EndDate.AddDays(1),
                        EndDate = newTrip.EndDate.AddDays(1),
                        OrderIndex = 3,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _segmentRepo.AddRangeAsync(baseSegments);
                // ✅ SINGLE COMMIT
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                _jobService.EnqueueWeatherPreload(newTrip.TripId);

                return newTrip;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}

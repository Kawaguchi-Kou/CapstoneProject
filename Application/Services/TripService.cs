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

        public TripService(ITripRepository tripRepo, IBackgroundJobService jobService)
        {
            _tripRepo = tripRepo;
            _jobService = jobService;
        }


        public async Task<Trip> CreateTripAsync(Trip newTrip)
        {
            newTrip.Status = TripStatus.InProgress;
            newTrip.CreatedAt = DateTime.UtcNow;
            await _tripRepo.AddAsync(newTrip);
            _jobService.EnqueueWeatherPreload(newTrip.TripId);
            return newTrip;
        }
    }
}

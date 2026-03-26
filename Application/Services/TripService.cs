using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Weather;

namespace Application.Services
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepo;
        private readonly ILocationRepository _locationRepo;
        private readonly IWeatherForecastRepository _weatherRepo;
        private readonly IAdaptiveWeatherRiskEngine _riskEngine;

        public TripService(ITripRepository tripRepo)
        {
            _tripRepo = tripRepo;
        }


        public async Task<Trip> CreateTripAsync(Trip newTrip)
        {
            newTrip.Status = TripStatus.InProgress;
            newTrip.CreatedAt = DateTime.UtcNow;
            await _tripRepo.AddAsync(newTrip);
            return newTrip;
        }
    }
}

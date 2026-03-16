using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;

        public LocationService(ILocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
        }

        public async Task<List<Location>> GetAllAsync()
        {
            return await _locationRepository.GetAllAsync();
        }

        public async Task<Location?> GetByIdAsync(Guid id)
        {
            return await _locationRepository.GetByIdAsync(id);
        }

        public async Task<Location> CreateAsync(CreateLocationRequest request)
        {
            var location = new Location
            {
                LocationId = Guid.NewGuid(),
                LocationName = request.LocationName,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            await _locationRepository.AddAsync(location);

            return location;
        }

        public async Task<Location> UpdateAsync(Guid id, UpdateLocationRequest request)
        {
            var location = await _locationRepository.GetByIdAsync(id);

            if (location == null)
                throw new Exception("Location not found");

            location.LocationName = request.LocationName ?? location.LocationName;
            location.Latitude = request.Latitude ?? location.Latitude;
            location.Longitude = request.Longitude ?? location.Longitude;

            await _locationRepository.UpdateAsync(location);

            return location;
        }

        public async Task DeleteAsync(Guid id)
        {
            var location = await _locationRepository.GetByIdAsync(id);

            if (location == null)
                throw new Exception("Location not found");

            await _locationRepository.DeleteAsync(location);
        }
    }
}

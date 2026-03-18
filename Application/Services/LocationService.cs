using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.ComponentModel;

namespace Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;

        public LocationService(ILocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
        }

        public async Task<List<LocationResponse>> GetAllAsync()
        {
            var locations = await _locationRepository.GetAllAsync();

            return locations.Select(x => new LocationResponse
            {
                LocationId = x.LocationId,
                LocationName = x.LocationName,
                Latitude = x.Latitude,
                Longitude = x.Longitude
            }).ToList();
        }

        public async Task<LocationResponse?> GetByIdAsync(Guid id)
        {
            var location = await _locationRepository.GetByIdAsync(id);

            if (location == null)
                return null;

            return new LocationResponse
            {
                LocationId = location.LocationId,
                LocationName = location.LocationName,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };
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

        public async Task ImportExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            List<Location> locations = new();

            // lấy số lượng record hiện tại
            var currentCount = (await _locationRepository.GetAllAsync()).Count;

            for (int row = 2; row <= rowCount; row++)
            {
                string name = worksheet.Cells[row, 1].Text;

                if (!double.TryParse(worksheet.Cells[row, 2].Text, out double latitude))
                    continue;

                if (!double.TryParse(worksheet.Cells[row, 3].Text, out double longitude))
                    continue;

                var location = new Location
                {
                    LocationId = Guid.NewGuid(), 
                    LocationName = name,
                    Latitude = latitude,
                    Longitude = longitude
                };

                locations.Add(location);
            }

            foreach (var location in locations)
            {
                await _locationRepository.AddAsync(location);
            }
        }
    }
}

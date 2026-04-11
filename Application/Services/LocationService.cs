using System.ComponentModel;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Helper;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

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

            var existingLocations = await _locationRepository.GetAllAsync();

            var existingKeys = existingLocations
                .Select(x => StringNormalizer.Normalize(x.LocationName))
                .ToHashSet();

            List<Location> newLocations = new();

            for (int row = 2; row <= rowCount; row++)
            {
                string name = worksheet.Cells[row, 1].Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    throw new Exception($"Row {row}: Location name is empty");

                if (!double.TryParse(worksheet.Cells[row, 2].Text, out double lat))
                    throw new Exception($"Row {row}: Invalid latitude");

                if (!double.TryParse(worksheet.Cells[row, 3].Text, out double lng))
                    throw new Exception($"Row {row}: Invalid longitude");

                var normalizedKey = StringNormalizer.Normalize(name);

                if (existingKeys.Contains(normalizedKey))
                    throw new Exception($"Row {row}: Duplicate location '{name}'");

                var location = new Location
                {
                    LocationId = Guid.NewGuid(),
                    LocationName = name,
                    Latitude = lat,
                    Longitude = lng
                };

                newLocations.Add(location);
                existingKeys.Add(normalizedKey);
            }

            if (newLocations.Any())
            {
                await _locationRepository.AddRangeAsync(newLocations);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class DistrictService : IDistrictService
    {
        private readonly IDistrictRepository _districtRepository;

        public DistrictService(IDistrictRepository districtRepository)
        {
            _districtRepository = districtRepository;
        }

        public async Task<List<District>> GetAllAsync()
        {
            return await _districtRepository.GetAllAsync();
        }

        public async Task<List<District>> GetByLocationIdAsync(Guid locationId)
        {
            return await _districtRepository.GetByLocationIdAsync(locationId);
        }

        public async Task<District> CreateAsync(string name, Guid locationId)
        {
            var normalizedName = name.Trim();
            var existingDistrict = await _districtRepository.GetByNameAndLocationIdAsync(normalizedName, locationId);

            if (existingDistrict != null)
                throw new Exception("District already exists in this location");

            var district = new District
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                LocationId = locationId
            };

            await _districtRepository.AddAsync(district);

            return district;
        }

        public async Task<District> UpdateAsync(Guid id, string name, Guid locationId)
        {
            var district = await _districtRepository.GetByIdAsync(id);
            if (district == null)
                throw new KeyNotFoundException("District not found");

            var normalizedName = name.Trim();
            var existingDistrict = await _districtRepository.GetByNameAndLocationIdAsync(normalizedName, locationId);

            if (existingDistrict != null && existingDistrict.Id != id)
                throw new Exception("District already exists in this location");

            district.Name = normalizedName;
            district.LocationId = locationId;

            await _districtRepository.UpdateAsync(district);
            return district;
        }

        public async Task DeleteAsync(Guid id)
        {
            var district = await _districtRepository.GetByIdAsync(id);
            if (district == null)
                throw new KeyNotFoundException("District not found");

            await _districtRepository.DeleteAsync(district);
        }
    }
}

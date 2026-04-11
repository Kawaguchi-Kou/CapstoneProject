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
    }
}

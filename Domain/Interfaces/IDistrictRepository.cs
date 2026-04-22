using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IDistrictRepository
    {
        Task<District?> GetByIdAsync(Guid id);
        Task<List<District>> GetAllAsync();
        Task<List<District>> GetByLocationIdAsync(Guid locationId);
        Task<District?> GetByNameAndLocationIdAsync(string name, Guid locationId);
        Task AddAsync(District district);
    }
}

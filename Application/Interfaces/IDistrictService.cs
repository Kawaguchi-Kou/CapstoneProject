using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IDistrictService
    {
        Task<List<District>> GetAllAsync();
        Task<List<District>> GetByLocationIdAsync(Guid locationId);
        Task<District> CreateAsync(string name, Guid locationId);
        Task<District> UpdateAsync(Guid id, string name, Guid locationId);
        Task DeleteAsync(Guid id);
    }
}

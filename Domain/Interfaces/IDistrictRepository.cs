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
    }
}

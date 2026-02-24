using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPOIRepository
    {
        Task<POI?> GetByIdAsync(Guid poiId);
        Task<List<POI>> GetAllWithPreferencesAsync();
    }
}

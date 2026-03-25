using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPOIRepository
    {
        Task<POI?> GetByIdAsync(Guid poiId);
        Task<List<POI>> GetAllWithPreferencesAsync();
        Task<List<POI>> GetAllAsync();
        Task AddAsync(POI poi, List<Guid> preferenceIds);
        Task AddRangeAsync(List<POI> pois);

        Task<Location?> GetLocationByIdAsync(Guid locationId);
        Task UpdateAsync(POI poi);
        Task DeleteAsync(POI poi);
        Task<POI?> GetByNameAndCityAsync(string name, string city);

        Task<List<POI>> GetByLocationAsync(Guid locationId);

    }
}

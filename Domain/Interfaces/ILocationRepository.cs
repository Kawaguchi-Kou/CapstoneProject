
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ILocationRepository
    {


        Task AddAsync(Location location);
        Task UpdateAsync(Location location);
        Task DeleteAsync(Location location);
        Task AddRangeAsync(List<Location> locations);
        Task<Location> GetByNameAsync(string locationName);
        Task<List<Location>> GetByNamesAsync(List<string> names);

        Task<Location> GetByIdAsync(Guid locationId);
        Task<List<Location>> GetAllAsync();
        Task<List<Location>> GetByIdsAsync(List<Guid> locationIds);

        Task<Dictionary<Guid, Location>> GetByIdsAsDictionaryAsync(List<Guid> locationIds);
    }
}

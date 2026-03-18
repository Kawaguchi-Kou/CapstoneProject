
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

        Task<Location> GetByIdAsync(Guid locationId);
        Task<List<Location>> GetAllAsync();

    }
}

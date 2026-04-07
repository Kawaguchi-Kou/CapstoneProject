
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly AppDbContext _context;

        public LocationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Location location)
        {
            await _context.Locations.AddAsync(location);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<Location> locations)
        {
            await _context.Locations.AddRangeAsync(locations);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Location location)
        {
            _context.Locations.Update(location);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Location location)
        {
            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
        }
        public async Task<Location> GetByIdAsync(Guid locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);

            if (location == null) {
                throw new KeyNotFoundException($"Location not found.");
            }

            return location;
        }

        public async Task<Location> GetByNameAsync(string locationName)
        {
            var location = await _context.Locations.FirstOrDefaultAsync(x => x.LocationName == locationName);

            if (location == null)
            {
                throw new KeyNotFoundException($"Location not found.");
            }

            return location;
        }

        public async Task<List<Location>> GetAllAsync()
        {
            return await _context.Locations.ToListAsync();
        }

        public async Task<List<Location>> GetByIdsAsync(List<Guid> locationIds)
        {
            return await _context.Locations
                .Where(x => locationIds.Contains(x.LocationId))
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, Location>> GetByIdsAsDictionaryAsync(List<Guid> locationIds)
        {
            if (locationIds == null || !locationIds.Any())
                return new Dictionary<Guid, Location>();

            locationIds = locationIds.Distinct().ToList();

            var result =  await _context.Locations
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.LocationId))
                .ToDictionaryAsync(x => x.LocationId);

            if (result.Count != locationIds.Count)
            {
                var missingIds = locationIds.Except(result.Keys);
                throw new Exception($"Locations not found: {string.Join(", ", missingIds)}");
            }

            return result;
        }
    }
}

using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly AppDbContext _context;

        public LocationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Location>> GetAllAsync()
        {
            return await _context.Locations.ToListAsync();
        }

        public async Task<Location?> GetByIdAsync(Guid id)
        {
            return await _context.Locations
                .FirstOrDefaultAsync(x => x.LocationId == id);
        }

        public async Task AddAsync(Location location)
        {
            await _context.Locations.AddAsync(location);
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
    }
}

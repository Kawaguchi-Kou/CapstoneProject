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
    public class POIRepository : IPOIRepository
    {
        private readonly AppDbContext _context;

        public POIRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<POI?> GetByIdAsync(Guid poiId)
        {
            return await _context.POIs
                .FirstOrDefaultAsync(p => p.Id == poiId);
        }

        public async Task<List<POI>> GetAllWithPreferencesAsync()
        {
            return await _context.POIs
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .ToListAsync();
        }

        public async Task<List<POI>> GetAllAsync()
        {
            return await _context.POIs.ToListAsync();
        }

        public async Task AddAsync(POI poi)
        {
            await _context.POIs.AddAsync(poi);
            await _context.SaveChangesAsync();
        }

        public async Task<Location?> GetLocationByIdAsync(Guid locationId)
        {
            return await _context.Locations
                .FirstOrDefaultAsync(x => x.LocationId == locationId);
        }

        public async Task UpdateAsync(POI poi)
        {
            _context.POIs.Update(poi);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(POI poi)
        {
            _context.POIs.Remove(poi);
            await _context.SaveChangesAsync();
        }

        public async Task<POI?> GetByNameAndCityAsync(string name, string city)
        {
            name = name.Trim().ToLower();
            city = city.Trim().ToLower();

            return await _context.POIs
                .FirstOrDefaultAsync(p =>
                    p.Name.ToLower() == name &&
                    p.City.ToLower() == city);
        }

        public async Task<List<POI>> GetByLocationAsync(Guid locationId)
        {
            return await _context.POIs
                .Where(x => x.LocationId == locationId)
                .ToListAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
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
                .FirstOrDefaultAsync(p => p.Id == poiId && p.Status == POIStatus.Approved);
        }

        public async Task<List<POI>> GetAllWithPreferencesAsync()
        {
            return await _context.POIs
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.Status == POIStatus.Approved)
                .ToListAsync();
        }

        public async Task<List<POI>> GetAllAsync()
        {
            return await _context.POIs.Where(p => p.Status == POIStatus.Approved).ToListAsync();
        }

        public async Task AddAsync(POI poi, List<Guid> preferenceIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                poi.Status = POIStatus.Approved;

                await _context.POIs.AddAsync(poi);
                await _context.SaveChangesAsync();

                if (preferenceIds != null && preferenceIds.Any())
                {
                    var uniqueIds = preferenceIds.Distinct();

                    var poiPreferences = uniqueIds.Select(prefId => new POIPreference
                    {
                        PoiId = poi.Id,
                        PreferenceId = prefId
                    });

                    await _context.POIPreferences.AddRangeAsync(poiPreferences);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task AddRangeAsync(List<POI> pois)
        {
            await _context.POIs.AddRangeAsync(pois);
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
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p =>
                    p.Name.ToLower() == name &&
                    p.Location.LocationName.ToLower() == city &&
                    p.Status == POIStatus.Approved);
        }
        public async Task<List<Location>> GetAllLocationsAsync()
        {
            return await _context.Locations.ToListAsync();
        }

        public async Task<List<POI>> GetByLocationAsync(Guid locationId)
        {
            return await _context.POIs
                .Where(x => x.LocationId == locationId && x.Status == POIStatus.Approved)
                .ToListAsync();
        }
    }
}

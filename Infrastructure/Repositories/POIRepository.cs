using System.Linq;
using Application.DTOs.Requests;
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
                .Include(p => p.Location)
                .Include(p => p.Partner)
                    .ThenInclude(u => u.PartnerProfile)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .FirstOrDefaultAsync(p => p.Id == poiId);
        }

        public async Task<List<POI>> GetAllWithPreferencesAsync()
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.District)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.Status == POIStatus.Active)
                .ToListAsync();
        }

        public async Task<List<POI>> GetAllAsync()
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.Partner)
                    .ThenInclude(u => u.PartnerProfile)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<POI>> GetPoisByDistrictAsync(
        Guid locationId,
        Guid districtId)
        {
            return await _context.POIs
                .AsNoTracking()
                .Include(x => x.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(x =>
                    x.Status == POIStatus.Active &&
                    x.LocationId == locationId &&
                    x.DistrictId == districtId)
                .ToListAsync();
        }

        public async Task<List<POI>> GetByLocationDistrictPairsAsync(
    List<(Guid LocationId, Guid? DistrictId)> keys)
        {
            if (keys == null || !keys.Any())
                return new List<POI>();

            var locationIds = keys.Select(x => x.LocationId).Distinct().ToList();
            var districtIds = keys.Select(x => x.DistrictId).Distinct().ToList();

            var raw = await _context.POIs
                .AsNoTracking()
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p =>
                    locationIds.Contains(p.LocationId) &&
                    districtIds.Contains(p.DistrictId))
                .ToListAsync();

            // 🔥 exact pair filtering
            var keySet = keys.ToHashSet();

            return raw
                .Where(p => keySet.Contains((p.LocationId, p.DistrictId)))
                .ToList();
        }



        public async Task<List<POI>> GetByPartnerIdAsync(Guid partnerId)
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.PartnerId == partnerId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<POI>> GetByPartnerIdAsync(Guid partnerId, int skip, int take)
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.PartnerId == partnerId)
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByPartnerIdAsync(Guid partnerId)
        {
            return await _context.POIs
                .Where(p => p.PartnerId == partnerId)
                .CountAsync();
        }

        public async Task<List<POI>> GetPendingPartnerPoisAsync()
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.Partner)
                    .ThenInclude(u => u.PartnerProfile)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.PartnerId != null && p.Status == POIStatus.Pending)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<POI>> GetPendingPartnerPoisAsync(int skip, int take)
        {
            return await _context.POIs
                .Include(p => p.Location)
                .Include(p => p.Partner)
                    .ThenInclude(u => u.PartnerProfile)
                .Include(p => p.PoiPreferences)
                    .ThenInclude(pp => pp.Preference)
                .Where(p => p.PartnerId != null && p.Status == POIStatus.Pending)
                .OrderByDescending(p => p.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountPendingPartnerPoisAsync()
        {
            return await _context.POIs
                .Where(p => p.PartnerId != null && p.Status == POIStatus.Pending)
                .CountAsync();
        }

        public async Task AddAsync(POI poi, List<Guid> preferenceIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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

        public async Task<POI?> GetByNameAndCityAsync(string name, string city)
        {
            name = name.Trim().ToLower();
            city = city.Trim().ToLower();

            return await _context.POIs
                .FirstOrDefaultAsync(p =>
                    p.Name.ToLower() == name &&
                    p.Location.LocationName.ToLower() == city &&
                    p.Status == POIStatus.Active);
        }

        public async Task<List<POI>> GetByLocationAsync(Guid locationId)
        {
            return await _context.POIs
                .Where(x => x.LocationId == locationId && x.Status == POIStatus.Active)
                .ToListAsync();
        }

        public async Task<List<POI>> GetAllWithLocationDistrictAsync()
        {
            return await _context.POIs
                .Include(x => x.Location)
                .Include(x => x.District)
                .ToListAsync();
        }
    }
}

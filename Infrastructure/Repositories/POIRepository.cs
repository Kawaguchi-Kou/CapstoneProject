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

    }
}

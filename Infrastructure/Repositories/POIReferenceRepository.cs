using Domain.Entities;
using Infrastructure.EntitiesConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class POIPreferenceRepository : IPOIPreferenceRepository
    {
        private readonly AppDbContext _context;

        public POIPreferenceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<POIPreference> list)
        {
            await _context.POIPreferences.AddRangeAsync(list);
            await _context.SaveChangesAsync();
        }
    }
}

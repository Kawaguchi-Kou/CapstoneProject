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
    public class DistrictRepository : IDistrictRepository
    {
        private readonly AppDbContext _context;

        public DistrictRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<District?> GetByIdAsync(Guid id)
        {
            return await _context.Districts.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<District>> GetAllAsync()
        {
            return await _context.Districts.ToListAsync();
        }

        public async Task<List<District>> GetByLocationIdAsync(Guid locationId)
        {
            return await _context.Districts
                .Where(d => d.LocationId == locationId)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}

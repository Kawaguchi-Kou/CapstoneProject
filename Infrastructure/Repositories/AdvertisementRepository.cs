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
    public class AdvertisementRepository : IAdvertisementRepository
    {
        private readonly AppDbContext _context;

        public AdvertisementRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Advertisement?> GetByIdAsync(Guid adId)
        {
            return await _context.Advertisements
                .Include(a => a.Account)
                .Include(a => a.Package)
                .Include(a => a.POI)
                .FirstOrDefaultAsync(a => a.AdId == adId);
        }

        public async Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Advertisements
                .Include(a => a.Package)
                .Include(a => a.POI)
                .Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Advertisement> CreateAsync(Advertisement advertisement)
        {
            advertisement.AdId = Guid.NewGuid();
            advertisement.CreatedAt = DateTime.UtcNow;
            await _context.Advertisements.AddAsync(advertisement);
            await _context.SaveChangesAsync();
            return advertisement;
        }

        public async Task<Advertisement> UpdateAsync(Advertisement advertisement)
        {
            _context.Advertisements.Update(advertisement);
            await _context.SaveChangesAsync();
            return advertisement;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

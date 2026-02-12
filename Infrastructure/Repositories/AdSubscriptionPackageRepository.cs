using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;

namespace Infrastructure.Repositories
{
    public class AdSubscriptionPackageRepository : IAdSubscriptionPackageRepository
    {
        private readonly AppDbContext _context;

        public AdSubscriptionPackageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdSubscriptionPackage?> GetByIdAsync(Guid packageId)
        {
            return await _context.adSubscriptionPackages
                .FirstOrDefaultAsync(p => p.PackageId == packageId);
        }

        public async Task<List<AdSubscriptionPackage>> GetAllAsync()
        {
            return await _context.adSubscriptionPackages
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<AdSubscriptionPackage> CreateAsync(AdSubscriptionPackage package)
        {
            package.PackageId = Guid.NewGuid();
            package.CreatedAt = DateTime.UtcNow;
            await _context.adSubscriptionPackages.AddAsync(package);
            await _context.SaveChangesAsync();
            return package;
        }

        public async Task<AdSubscriptionPackage> UpdateAsync(AdSubscriptionPackage package)
        {
            _context.adSubscriptionPackages.Update(package);
            await _context.SaveChangesAsync();
            return package;
        }

        public async Task<bool> DeleteAsync(Guid packageId)
        {
            var package = await GetByIdAsync(packageId);
            if (package == null)
                return false;

            _context.adSubscriptionPackages.Remove(package);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

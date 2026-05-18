using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PartnerProfileRepository : IPartnerProfileRepository
    {
        private readonly AppDbContext _context;

        public PartnerProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PartnerProfile> CreateAsync(PartnerProfile profile)
        {
            await _context.PartnerProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<PartnerProfile?> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.PartnerProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId);
        }

        public async Task<PartnerProfile?> GetByIdAsync(Guid id)
        {
            return await _context.PartnerProfiles
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PartnerProfile> UpdateAsync(PartnerProfile profile)
        {
            _context.PartnerProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

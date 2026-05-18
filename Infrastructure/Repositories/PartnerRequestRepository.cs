using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PartnerRequestRepository : IPartnerRequestRepository
    {
        private readonly AppDbContext _context;

        public PartnerRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PartnerRequest> CreateAsync(PartnerRequest request)
        {
            await _context.PartnerRequests.AddAsync(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<PartnerRequest?> GetByIdAsync(Guid id)
        {
            return await _context.PartnerRequests
                .Include(r => r.Account)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PartnerRequest?> GetLatestByAccountIdAsync(Guid accountId)
        {
            return await _context.PartnerRequests
                .Where(r => r.AccountId == accountId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasPendingRequestAsync(Guid accountId)
        {
            return await _context.PartnerRequests
                .AnyAsync(r => r.AccountId == accountId && r.Status == PartnerRequestStatus.Pending);
        }

        public async Task<List<PartnerRequest>> GetByStatusAsync(PartnerRequestStatus status, int skip, int take)
        {
            return await _context.PartnerRequests
                .AsNoTracking()
                .Include(r => r.Account)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByStatusAsync(PartnerRequestStatus status)
        {
            return await _context.PartnerRequests
                .AsNoTracking()
                .CountAsync(r => r.Status == status);
        }

        public async Task<PartnerRequest> UpdateAsync(PartnerRequest request)
        {
            _context.PartnerRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<List<PartnerRequest>> GetAllAsync()
        {
            return await _context.PartnerRequests.AsNoTracking().ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

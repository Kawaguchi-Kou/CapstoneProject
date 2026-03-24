using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

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
                .Include(a => a.Promotion)
                .FirstOrDefaultAsync(a => a.AdId == adId);
        }

        public async Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Advertisements
                .Include(a => a.Package)
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Advertisement> CreateWithPromotionAsync(Advertisement advertisement, Promotion promotion)
        {
            advertisement.AdId = Guid.NewGuid();
            advertisement.CreatedAt = DateTime.UtcNow;

            promotion.PromotionId = Guid.NewGuid();
            promotion.AdId = advertisement.AdId;
            promotion.CreatedAt = DateTime.UtcNow;

            await _context.Advertisements.AddAsync(advertisement);
            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            return advertisement;
        }

        public async Task<Advertisement> UpdateAsync(Advertisement advertisement)
        {
            _context.Advertisements.Update(advertisement);
            await _context.SaveChangesAsync();
            return advertisement;
        }

        public async Task<Promotion?> GetPromotionByIdAsync(Guid promotionId)
        {
            return await _context.Promotions
                .Include(p => p.Advertisement)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId);
        }

        public async Task<List<SavedPromotion>> GetSavedPromotionsByAccountIdAsync(Guid accountId)
        {
            return await _context.SavedPromotions
                .Include(sp => sp.Promotion)
                    .ThenInclude(p => p.Advertisement)
                .Where(sp => sp.AccountId == accountId)
                .OrderByDescending(sp => sp.SavedAt)
                .ToListAsync();
        }

        public async Task<bool> IsPromotionSavedAsync(Guid accountId, Guid promotionId)
        {
            return await _context.SavedPromotions
                .AnyAsync(sp => sp.AccountId == accountId && sp.PromotionId == promotionId);
        }

        public async Task SavePromotionAsync(SavedPromotion savedPromotion)
        {
            await _context.SavedPromotions.AddAsync(savedPromotion);
        }

        public async Task<List<(Account Account, int PendingAdsCount, DateTime LatestPendingAt)>> GetPendingAccountsAsync(
            int skip,
            int take,
            string? search = null)
        {
            var query = _context.Advertisements
                .Include(a => a.Account)
                .Where(a => a.Status == AdStatus.PendingApproval);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.Account.Email != null && a.Account.Email.ToLower().Contains(normalized)) ||
                    (a.Account.Name != null && a.Account.Name.ToLower().Contains(normalized)) ||
                    (a.Account.PhoneNumber != null && a.Account.PhoneNumber.ToLower().Contains(normalized)));
            }

            var grouped = await query
                .GroupBy(a => a.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    PendingAdsCount = g.Count(),
                    LatestPendingAt = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.LatestPendingAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var accountIds = grouped.Select(g => g.AccountId).ToList();
            var accounts = await _context.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a);

            return grouped
                .Where(g => accounts.ContainsKey(g.AccountId))
                .Select(g => (accounts[g.AccountId], g.PendingAdsCount, g.LatestPendingAt))
                .ToList();
        }

        public async Task<int> CountPendingAccountsAsync(string? search = null)
        {
            var query = _context.Advertisements
                .Include(a => a.Account)
                .Where(a => a.Status == AdStatus.PendingApproval);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.Account.Email != null && a.Account.Email.ToLower().Contains(normalized)) ||
                    (a.Account.Name != null && a.Account.Name.ToLower().Contains(normalized)) ||
                    (a.Account.PhoneNumber != null && a.Account.PhoneNumber.ToLower().Contains(normalized)));
            }

            return await query
                .Select(a => a.AccountId)
                .Distinct()
                .CountAsync();
        }

        public async Task<List<Advertisement>> GetPendingByAccountIdAsync(Guid accountId, int skip, int take, string? keyword = null)
        {
            var query = _context.Advertisements
                .Include(a => a.Package)
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.AccountId == accountId && a.Status == AdStatus.PendingApproval);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim().ToLower();
                query = query.Where(a =>
                    (a.Title != null && a.Title.ToLower().Contains(normalized)) ||
                    (a.POI != null && a.POI.Name != null && a.POI.Name.ToLower().Contains(normalized)));
            }

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountPendingByAccountIdAsync(Guid accountId, string? keyword = null)
        {
            var query = _context.Advertisements
                .Include(a => a.POI)
                .Where(a => a.AccountId == accountId && a.Status == AdStatus.PendingApproval);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim().ToLower();
                query = query.Where(a =>
                    (a.Title != null && a.Title.ToLower().Contains(normalized)) ||
                    (a.POI != null && a.POI.Name != null && a.POI.Name.ToLower().Contains(normalized)));
            }

            return await query.CountAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Advertisement>> GetAllAsync()
        {
            return await _context.Advertisements
                .Include(a => a.Account)
                .Include(a => a.Package)
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetPendingAsync()
        {
            return await _context.Advertisements
                .Include(a => a.Account)
                .Include(a => a.Package)
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.PendingApproval)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetActiveAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.Active
                    && a.StartDate <= now
                    && a.EndDate >= now
                    && a.Promotion != null
                    && a.Promotion.Status == PromotionStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}

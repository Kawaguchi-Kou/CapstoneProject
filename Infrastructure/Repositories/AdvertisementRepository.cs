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
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .FirstOrDefaultAsync(a => a.AdId == adId);
        }

        public async Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Advertisement> CreateWithPromotionAsync(Advertisement advertisement, Promotion promotion)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Advertisements.AddAsync(advertisement);
                await _context.SaveChangesAsync();

                promotion.AdId = advertisement.AdId;
                await _context.Promotions.AddAsync(promotion);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                advertisement.Promotion = promotion;
                return advertisement;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
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
                        .ThenInclude(a => a.POI)
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
            await _context.SaveChangesAsync();
        }

        public async Task<List<(Account Account, int PendingAdsCount, DateTime LatestPendingAt)>> GetPendingAccountsAsync(
            int skip,
            int take,
            string? search = null)
        {
            var pendingAds = _context.Advertisements
                .Where(a => a.Status == AdStatus.PendingApproval)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                pendingAds = pendingAds.Where(a =>
                    a.Account.Email.ToLower().Contains(keyword) ||
                    a.Account.Name.ToLower().Contains(keyword));
            }

            var grouped = await pendingAds
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

            var accountIds = grouped.Select(x => x.AccountId).ToList();
            var accounts = await _context.Accounts
                .Where(a => accountIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);

            return grouped
                .Where(x => accounts.ContainsKey(x.AccountId))
                .Select(x => (accounts[x.AccountId], x.PendingAdsCount, x.LatestPendingAt))
                .ToList();
        }

        public async Task<int> CountPendingAccountsAsync(string? search = null)
        {
            var pendingAds = _context.Advertisements
                .Where(a => a.Status == AdStatus.PendingApproval)
                .Include(a => a.Account)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                pendingAds = pendingAds.Where(a =>
                    a.Account.Email.ToLower().Contains(keyword) ||
                    a.Account.Name.ToLower().Contains(keyword));
            }

            return await pendingAds
                .Select(a => a.AccountId)
                .Distinct()
                .CountAsync();
        }

        public async Task<List<Advertisement>> GetPendingByAccountIdAsync(
            Guid accountId,
            int skip,
            int take,
            string? keyword = null)
        {
            var query = _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.AccountId == accountId && a.Status == AdStatus.PendingApproval)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(k) ||
                    a.Content.ToLower().Contains(k) ||
                    (a.POI != null && a.POI.Name.ToLower().Contains(k)));
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
                .Where(a => a.AccountId == accountId && a.Status == AdStatus.PendingApproval)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(k) ||
                    a.Content.ToLower().Contains(k) ||
                    (a.POI != null && a.POI.Name.ToLower().Contains(k)));
            }

            return await query.CountAsync();
        }

        public async Task<int> CountActiveByPoiIdAsync(Guid poiId)
        {
            return await _context.Advertisements
                .CountAsync(a => a.POIId == poiId && a.Status == AdStatus.Active);
        }

        public async Task<int> InactivateActiveByPoiIdAsync(Guid poiId)
        {
            var activeAds = await _context.Advertisements
                .Where(a => a.POIId == poiId && a.Status == AdStatus.Active)
                .ToListAsync();

            foreach (var ad in activeAds)
            {
                ad.Status = AdStatus.Expired;
                if (ad.Promotion != null && ad.Promotion.Status == PromotionStatus.Active)
                {
                    ad.Promotion.Status = PromotionStatus.Expired;
                    ad.Promotion.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return activeAds.Count;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Advertisement>> GetAllAsync()
        {
            return await _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetPendingAsync()
        {
            return await _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.PendingApproval)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetActiveAsync()
        {
            return await _context.Advertisements
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}

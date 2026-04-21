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

        public async Task<List<(Guid AccountId, string Email, string Name, int PendingAdsCount)>> GetManagerAccountsAsync(
            int skip,
            int take,
            string? keyword = null)
        {
            var query = _context.Accounts
                .AsNoTracking()
                .Where(a => a.Role != null && a.Role.Name == "Partner")
                .Where(a => a.Advertisements.Any(ad => ad.Status == AdStatus.PendingApproval))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();
                query = query.Where(a =>
                    a.Email.ToLower().Contains(normalizedKeyword) ||
                    a.Name.ToLower().Contains(normalizedKeyword));
            }

            var rows = await query
                .OrderBy(a => a.Name)
                .ThenBy(a => a.Email)
                .Skip(skip)
                .Take(take)
                .Select(a => new
                {
                    a.Id,
                    a.Email,
                    a.Name,
                    PendingAdsCount = a.Advertisements.Count(ad => ad.Status == AdStatus.PendingApproval)
                })
                .ToListAsync();

            return rows
                .Select(x => (x.Id, x.Email, x.Name, x.PendingAdsCount))
                .ToList();
        }

        public async Task<int> CountManagerAccountsAsync(string? keyword = null)
        {
            var query = _context.Accounts
                .AsNoTracking()
                .Where(a => a.Role != null && a.Role.Name == "Partner")
                .Where(a => a.Advertisements.Any(ad => ad.Status == AdStatus.PendingApproval))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim().ToLower();
                query = query.Where(a =>
                    a.Email.ToLower().Contains(normalizedKeyword) ||
                    a.Name.ToLower().Contains(normalizedKeyword));
            }

            return await query.CountAsync();
        }

        public async Task<List<Advertisement>> GetByAccountIdAndStatusAsync(
            Guid accountId,
            AdStatus status,
            int skip,
            int take)
        {
            return await _context.Advertisements
                .AsNoTracking()
                .Include(a => a.POI)
                .Include(a => a.Promotion)
                .Where(a => a.AccountId == accountId && a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByAccountIdAndStatusAsync(Guid accountId, AdStatus status)
        {
            return await _context.Advertisements
                .AsNoTracking()
                .CountAsync(a => a.AccountId == accountId && a.Status == status);
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
                ad.Status = AdStatus.Paused;
                if (ad.Promotion != null && ad.Promotion.Status == PromotionStatus.Active)
                {
                    ad.Promotion.Status = PromotionStatus.Inactive;
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
                .Include(a => a.Account)
                .Include(a => a.POI)
                    .ThenInclude(p => p.PoiPreferences)
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.Active)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}

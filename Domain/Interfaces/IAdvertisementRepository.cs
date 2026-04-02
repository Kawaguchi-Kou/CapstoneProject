using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAdvertisementRepository
    {
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId);
        Task<Advertisement> CreateWithPromotionAsync(Advertisement advertisement, Promotion promotion);
        Task<Advertisement> UpdateAsync(Advertisement advertisement);

        Task<Promotion?> GetPromotionByIdAsync(Guid promotionId);
        Task<List<SavedPromotion>> GetSavedPromotionsByAccountIdAsync(Guid accountId);
        Task<bool> IsPromotionSavedAsync(Guid accountId, Guid promotionId);
        Task SavePromotionAsync(SavedPromotion savedPromotion);
        Task<int> CountActiveByPoiIdAsync(Guid poiId);
        Task InactivateActiveByPoiIdAsync(Guid poiId);

        Task<List<(Account Account, int PendingAdsCount, DateTime LatestPendingAt)>> GetPendingAccountsAsync(
            int skip,
            int take,
            string? search = null);
        Task<int> CountPendingAccountsAsync(string? search = null);

        Task<List<Advertisement>> GetPendingByAccountIdAsync(
            Guid accountId,
            int skip,
            int take,
            string? keyword = null);
        Task<int> CountPendingByAccountIdAsync(Guid accountId, string? keyword = null);

        Task SaveChangesAsync();
        Task<List<Advertisement>> GetAllAsync();
        Task<List<Advertisement>> GetPendingAsync();
        Task<List<Advertisement>> GetActiveAsync();
    }
}

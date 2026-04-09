using Domain.Entities;
using Domain.Enums;

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

        Task<List<(Guid AccountId, string Email, string Name, int PendingAdsCount)>> GetManagerAccountsAsync(
            int skip,
            int take,
            string? keyword = null);
        Task<int> CountManagerAccountsAsync(string? keyword = null);

        Task<List<Advertisement>> GetByAccountIdAndStatusAsync(
            Guid accountId,
            AdStatus status,
            int skip,
            int take);
        Task<int> CountByAccountIdAndStatusAsync(Guid accountId, AdStatus status);

        Task<int> CountActiveByPoiIdAsync(Guid poiId);
        Task<int> InactivateActiveByPoiIdAsync(Guid poiId);

        Task SaveChangesAsync();
        Task<List<Advertisement>> GetAllAsync();
        Task<List<Advertisement>> GetPendingAsync();
        Task<List<Advertisement>> GetActiveAsync();
    }
}

using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAdvertisementService
    {
        Task<Advertisement> CreateAdvertisementAsync(Guid accountId, CreateAdvertisementRequest request);
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId);
        Task<Advertisement> ApproveAdvertisementAsync(Guid adId);
        Task<Advertisement> RejectAdvertisementAsync(Guid adId, string? reason = null);
        Task<Advertisement> InactivateMyAdvertisementAsync(Guid accountId, Guid adId);
        Task<Advertisement> ActivateMyAdvertisementAsync(Guid accountId, Guid adId);

        Task<PagedResultResponse<PendingAdvertisementAccountItemResponse>> GetManagerAccountsAsync(
            int page,
            int pageSize,
            string? keyword = null);

        Task<PagedResultResponse<PendingAdvertisementItemResponse>> GetManagerAdvertisementsByAccountAsync(
            Guid accountId,
            string? status,
            int page,
            int pageSize);

        Task<List<Advertisement>> GetAllAsync();
        Task<List<Advertisement>> GetPendingAsync();
        Task<List<RecommendedAdsResponse>> GetActiveAsync(Guid? accountId = null);
        Task<List<SavedPromotion>> GetSavedPromotionsByAccountIdAsync(Guid accountId);
        Task SavePromotionAsync(Guid accountId, Guid promotionId);
    }
}

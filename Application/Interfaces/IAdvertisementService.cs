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

        Task<PagedResultResponse<PendingAdvertisementAccountItemResponse>> GetPendingAdvertisementAccountsAsync(
            int page,
            int pageSize,
            string? search = null);

        Task<PagedResultResponse<PendingAdvertisementItemResponse>> GetPendingAdvertisementsByAccountAsync(
            Guid accountId,
            int page,
            int pageSize,
            string? keyword = null);
        Task<List<Advertisement>> GetAllAsync();

        Task<List<Advertisement>> GetPendingAsync();


    }
}

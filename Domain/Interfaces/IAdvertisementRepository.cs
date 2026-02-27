using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    public interface IAdvertisementRepository
    {
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId);
        Task<Advertisement> CreateAsync(Advertisement advertisement);
        Task<Advertisement> UpdateAsync(Advertisement advertisement);

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
    }
}

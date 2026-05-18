using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    public interface IPartnerRequestRepository
    {
        Task<PartnerRequest> CreateAsync(PartnerRequest request);
        Task<PartnerRequest?> GetByIdAsync(Guid id);
        Task<PartnerRequest?> GetLatestByAccountIdAsync(Guid accountId);
        Task<bool> HasPendingRequestAsync(Guid accountId);
        Task<List<PartnerRequest>> GetByStatusAsync(PartnerRequestStatus status, int skip, int take);
        Task<int> CountByStatusAsync(PartnerRequestStatus status);
        Task<PartnerRequest> UpdateAsync(PartnerRequest request);
        Task<List<PartnerRequest>> GetAllAsync();
        Task SaveChangesAsync();
    }
}

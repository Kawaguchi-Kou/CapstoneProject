using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPartnerProfileRepository
    {
        Task<PartnerProfile> CreateAsync(PartnerProfile profile);
        Task<PartnerProfile?> GetByAccountIdAsync(Guid accountId);
        Task<PartnerProfile?> GetByIdAsync(Guid id);
        Task<PartnerProfile> UpdateAsync(PartnerProfile profile);
        Task SaveChangesAsync();
    }
}

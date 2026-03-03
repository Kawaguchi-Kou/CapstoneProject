using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<Account?> GetByIdAsync(Guid id);
        Task<List<Account>> GetAllAsync();
        Task<Account> UpdateProfileAsync(Account user);
        Task<Account> CreateProfileAsync(Account user);
        Task<List<Account>> GetByIdsAsync(List<Guid> ids);
        Task<List<UserPreference>> GetPreferenceByAccountIdAsync(Guid accountId);
        //Task UpsertAsync(Guid accountId, List<UserPreference> preferences);
        Task ReplaceUserPreferences(
            Guid accountId,
            List<Guid> preferenceIds);
        Task SaveChangesAsync();
    }
}

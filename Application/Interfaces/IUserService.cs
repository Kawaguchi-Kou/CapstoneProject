using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<Account?> GetById(Guid id);
        Task<List<Account>> GetAll();
        Task<Account> UpdateProfile(Account user);
        Task<Account> CreateProfile(Account user);
        Task<List<Account>> GetByIdsAsync(List<Guid> ids);
        Task UpdateUserPreferencesAsync(Guid accountId, UserPreferencesRequest request);
        Task<List<UserPreference>> GetUserPreferencesAsync(Guid accountId);
    }
}

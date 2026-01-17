using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAuthRepository
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account?> GetByIdAsync(Guid id);
        Task AddAsync(Account account);
        Task SaveChangesAsync();
        Task ChangePasswordAsync(Account account);
        Task<List<Account>> GetAllAccountsAsync();

    }
}

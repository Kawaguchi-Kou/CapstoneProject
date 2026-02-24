using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAccountSubscriptionRepository
    {
        Task<AccountSubscription?> GetByIdAsync(Guid subscriptionId);
        Task<AccountSubscription?> GetActiveByAccountIdAsync(Guid accountId);
        Task<List<AccountSubscription>> GetByAccountIdAsync(Guid accountId);
        Task<AccountSubscription> CreateAsync(AccountSubscription subscription);
        Task<AccountSubscription> UpdateAsync(AccountSubscription subscription);
        Task SaveChangesAsync();
    }
}

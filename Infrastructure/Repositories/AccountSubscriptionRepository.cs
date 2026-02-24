using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class AccountSubscriptionRepository : IAccountSubscriptionRepository
    {
        private readonly AppDbContext _context;

        public AccountSubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AccountSubscription?> GetByIdAsync(Guid subscriptionId)
        {
            return await _context.accountSubscriptions
                .Include(s => s.SubscriptionPackage)
                .Include(s => s.Account)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        }

        public async Task<AccountSubscription?> GetActiveByAccountIdAsync(Guid accountId)
        {
            return await _context.accountSubscriptions
                .Include(s => s.SubscriptionPackage)
                .Where(s => s.AccountId == accountId && s.Status == Domain.Enums.SubStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AccountSubscription>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.accountSubscriptions
                .Include(s => s.SubscriptionPackage)
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<AccountSubscription> CreateAsync(AccountSubscription subscription)
        {
            subscription.SubscriptionId = Guid.NewGuid();
            subscription.CreatedAt = DateTime.UtcNow;
            await _context.accountSubscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<AccountSubscription> UpdateAsync(AccountSubscription subscription)
        {
            _context.accountSubscriptions.Update(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

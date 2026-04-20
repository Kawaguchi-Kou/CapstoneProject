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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdPayment?> GetByIdAsync(Guid paymentId)
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<AdPayment?> GetByTransactionContentAsync(string transactionContent)
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.TransactionContent == transactionContent);
        }

        public async Task<List<AdPayment>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .Where(p => p.SubscriptionId == subscriptionId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();
        }

        public async Task<List<AdPayment>> GetByAccountIdAsync(Guid accountId)
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .Where(p => p.AccountId == accountId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();
        }

        public async Task<AdPayment> CreateAsync(AdPayment payment)
        {
            await _context.adPayments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<AdPayment> UpdateAsync(AdPayment payment)
        {
            _context.adPayments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

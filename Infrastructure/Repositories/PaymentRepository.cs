using Domain.Entities;
using Domain.Interfaces;
using Domain.Enums;
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

        public async Task<int> CountByAccountIdAsync(Guid accountId)
        {
            return await _context.adPayments.CountAsync(p => p.AccountId == accountId);
        }

        public async Task<List<AdPayment>> GetByAccountIdAsync(Guid accountId, int skip, int take)
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .Where(p => p.AccountId == accountId)
                .OrderByDescending(p => p.PaidAt)
                .ThenByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
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

        public async Task<int> ExpirePendingPaymentsAsync(DateTime utcNow)
        {
            var expiredPendingPayments = await _context.adPayments
                .Where(p => p.PaymentStatus == PaymentStatus.Pending && p.ExpiresAt <= utcNow)
                .ToListAsync();

            if (!expiredPendingPayments.Any())
            {
                return 0;
            }

            foreach (var payment in expiredPendingPayments)
            {
                payment.PaymentStatus = PaymentStatus.Failed;
            }

            await _context.SaveChangesAsync();
            return expiredPendingPayments.Count;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<AdPayment>> GetAllAsync()
        {
            return await _context.adPayments
                .Include(p => p.Subscription)
                .ThenInclude(s => s.SubscriptionPackage)
                .ToListAsync();
        }

        public async Task<int> CountAllAsync(string? status)
        {
            var query = _context.adPayments.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(p => p.PaymentStatus == parsedStatus);
                }
            }

            return await query.CountAsync();
        }

        public async Task<List<AdPayment>> GetAllPagedAsync(int skip, int take, string? status, string? sortOrder)
        {
            var query = _context.adPayments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.SubscriptionPackage)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(p => p.PaymentStatus == parsedStatus);
                }
            }

            query = sortOrder?.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt);

            return await query.Skip(skip).Take(take).ToListAsync();
        }
    }
}

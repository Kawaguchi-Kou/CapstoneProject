using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<AdPayment?> GetByIdAsync(Guid paymentId);
        Task<AdPayment?> GetByTransactionContentAsync(string transactionContent);
        Task<List<AdPayment>> GetBySubscriptionIdAsync(Guid subscriptionId);
        Task<List<AdPayment>> GetByAccountIdAsync(Guid accountId);
        Task<AdPayment> CreateAsync(AdPayment payment);
        Task<AdPayment> UpdateAsync(AdPayment payment);
        Task SaveChangesAsync();
    }
}

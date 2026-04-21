using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs
{
    public class PaymentExpiryJob : IPaymentExpiryJob
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentExpiryJob> _logger;

        public PaymentExpiryJob(IPaymentService paymentService, ILogger<PaymentExpiryJob> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task ExpirePendingPaymentsAsync()
        {
            var expiredCount = await _paymentService.ExpirePendingPaymentsAsync();
            if (expiredCount > 0)
            {
                _logger.LogInformation("⏳ Marked {Count} pending payments as failed", expiredCount);
            }
        }
    }
}

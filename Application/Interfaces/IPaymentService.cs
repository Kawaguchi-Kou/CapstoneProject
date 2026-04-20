using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreatePaymentAsync(Guid accountId, CreatePaymentRequest request);
        Task<AdPayment?> ProcessSePayWebhookAsync(SePayWebhookRequest webhookRequest);
        Task<AdPayment?> GetPaymentByIdAsync(Guid paymentId);
        Task<List<AdPayment>> GetPaymentsBySubscriptionIdAsync(Guid subscriptionId);
        Task<List<PaymentResponse>> GetPurchaseHistoryAsync(Guid accountId, string? userRole);
    }
}

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
        Task<PagedResultResponse<PaymentResponse>> GetPurchaseHistoryAsync(Guid accountId, string? userRole, int page = 1, int pageSize = 15);
        Task<PagedResultResponse<PaymentResponse>> GetAllTransactionsAsync(int page = 1, int pageSize = 15, string? status = null, string? sortOrder = null);
        Task<int> ExpirePendingPaymentsAsync();
    }
}

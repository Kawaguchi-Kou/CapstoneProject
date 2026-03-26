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
    public interface IAccountSubscriptionService
    {
        Task<AccountSubscriptionResponse> SubscribePackageAsync(Guid accountId, SubscribePackageRequest request);
        Task<AccountSubscription?> GetActiveSubscriptionAsync(Guid accountId);
        Task<List<AccountSubscription>> GetActiveSubscriptionsAsync(Guid accountId);
        Task<List<AccountSubscription>> GetAllSubscriptionsAsync(Guid accountId);
        Task<bool> CanCreateAdvertisementAsync(Guid accountId);
        Task IncrementAdsUsedAsync(Guid accountId);
        
        // Method cho Admin tạo subscription trực tiếp (không qua thanh toán)
        Task<AccountSubscriptionResponse> CreateSubscriptionDirectlyAsync(Guid accountId, SubscribePackageRequest request);
    }
}

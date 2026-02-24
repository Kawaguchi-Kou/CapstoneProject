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
        Task<bool> CanCreateAdvertisementAsync(Guid accountId);
        Task IncrementAdsUsedAsync(Guid accountId);
    }
}

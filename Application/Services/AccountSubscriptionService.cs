using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AccountSubscriptionService : IAccountSubscriptionService
    {
        private readonly IAccountSubscriptionRepository _subscriptionRepository;
        private readonly IAdSubscriptionPackageRepository _packageRepository;
        private readonly IAuthRepository _authRepository;

        public AccountSubscriptionService(
            IAccountSubscriptionRepository subscriptionRepository,
            IAdSubscriptionPackageRepository packageRepository,
            IAuthRepository authRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _packageRepository = packageRepository;
            _authRepository = authRepository;
        }

        public async Task<AccountSubscriptionResponse> SubscribePackageAsync(Guid accountId, SubscribePackageRequest request)
        {
            // 1. Kiểm tra package có tồn tại và active không
            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null)
                throw new KeyNotFoundException("Package not found");

            if (package.Status.ToLower() != "active")
                throw new InvalidOperationException("Package is not active");

            // 2. Kiểm tra account có tồn tại không
            var account = await _authRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Account not found");

            // 3. Kiểm tra account đã có subscription active chưa (optional: có thể cho phép nhiều subscription)
            var existingActive = await _subscriptionRepository.GetActiveByAccountIdAsync(accountId);
            if (existingActive != null)
            {
                // Có thể throw exception hoặc suspend subscription cũ
                // Ở đây tôi sẽ throw để đảm bảo chỉ có 1 subscription active tại 1 thời điểm
                throw new InvalidOperationException("Account already has an active subscription. Please wait for it to expire or contact support.");
            }

            // 4. Tạo subscription mới với MaxAds từ package
            var subscription = new AccountSubscription
            {
                AccountId = accountId,
                SubscriptionPackageId = request.PackageId,
                MaxAds = (int)package.MaxAdsPerPeriod,
                AdsUsed = 0,
                Status = SubStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _subscriptionRepository.CreateAsync(subscription);

            // 5. Load lại với navigation properties để map response
            var subscriptionWithNav = await _subscriptionRepository.GetByIdAsync(created.SubscriptionId);

            return new AccountSubscriptionResponse
            {
                SubscriptionId = subscriptionWithNav!.SubscriptionId,
                SubscriptionPackageId = subscriptionWithNav.SubscriptionPackageId,
                AccountId = subscriptionWithNav.AccountId,
                MaxAds = subscriptionWithNav.MaxAds,
                AdsUsed = subscriptionWithNav.AdsUsed,
                Status = subscriptionWithNav.Status,
                CreatedAt = subscriptionWithNav.CreatedAt,
                PackageTitle = subscriptionWithNav.SubscriptionPackage?.Title ?? string.Empty
            };
        }

        public async Task<AccountSubscription?> GetActiveSubscriptionAsync(Guid accountId)
        {
            return await _subscriptionRepository.GetActiveByAccountIdAsync(accountId);
        }

        public async Task<bool> CanCreateAdvertisementAsync(Guid accountId)
        {
            var subscription = await _subscriptionRepository.GetActiveByAccountIdAsync(accountId);
            
            if (subscription == null)
                return false;

            if (subscription.Status != SubStatus.Active)
                return false;

            return subscription.AdsUsed < subscription.MaxAds;
        }

        public async Task IncrementAdsUsedAsync(Guid accountId)
        {
            var subscription = await _subscriptionRepository.GetActiveByAccountIdAsync(accountId);
            
            if (subscription == null)
                throw new InvalidOperationException("No active subscription found for this account");

            if (subscription.AdsUsed >= subscription.MaxAds)
                throw new InvalidOperationException("Advertisement limit reached for this subscription");

            subscription.AdsUsed++;

            // Tự động expire subscription khi AdsUsed >= MaxAds
            if (subscription.AdsUsed >= subscription.MaxAds)
            {
                subscription.Status = SubStatus.Expired;
            }

            await _subscriptionRepository.UpdateAsync(subscription);
        }
    }
}

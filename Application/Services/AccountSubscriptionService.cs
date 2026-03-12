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

        private static bool IsExpiredByDuration(AccountSubscription subscription)
        {
            // DurationDays thuộc package. Nếu thiếu navigation/package invalid, coi như không active.
            if (subscription.SubscriptionPackage == null || subscription.SubscriptionPackage.DurationDays <= 0)
                return true;

            var expiredAtUtc = subscription.CreatedAt.AddDays(subscription.SubscriptionPackage.DurationDays);
            return DateTime.UtcNow >= expiredAtUtc;
        }

        private async Task<AccountSubscription?> GetAndRefreshActiveSubscriptionAsync(Guid accountId)
        {
            var subscription = await _subscriptionRepository.GetActiveByAccountIdAsync(accountId);
            if (subscription == null)
                return null;

            // Tự động expire nếu đã quá hạn theo DurationDays
            if (IsExpiredByDuration(subscription))
            {
                subscription.Status = SubStatus.Expired;
                await _subscriptionRepository.UpdateAsync(subscription);
                return null;
            }

            return subscription;
        }

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
            var existingActive = await GetAndRefreshActiveSubscriptionAsync(accountId);
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
            return await GetAndRefreshActiveSubscriptionAsync(accountId);
        }

        public async Task<List<AccountSubscription>> GetActiveSubscriptionsAsync(Guid accountId)
        {
            var subscriptions = await _subscriptionRepository.GetByAccountIdAsync(accountId);

            // Lọc những subscription còn active và chưa expired theo DurationDays
            var activeSubscriptions = new List<AccountSubscription>();

            foreach (var subscription in subscriptions)
            {
                if (subscription.Status != SubStatus.Active)
                    continue;

                if (IsExpiredByDuration(subscription))
                {
                    subscription.Status = SubStatus.Expired;
                    await _subscriptionRepository.UpdateAsync(subscription);
                    continue;
                }

                activeSubscriptions.Add(subscription);
            }

            return activeSubscriptions;
        }

        public async Task<bool> CanCreateAdvertisementAsync(Guid accountId)
        {
            var subscription = await GetAndRefreshActiveSubscriptionAsync(accountId);

            if (subscription == null)
                return false;

            if (subscription.Status != SubStatus.Active)
                return false;

            return subscription.AdsUsed < subscription.MaxAds;
        }

        public async Task IncrementAdsUsedAsync(Guid accountId)
        {
            var subscription = await GetAndRefreshActiveSubscriptionAsync(accountId);

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

        /// <summary>
        /// Tạo subscription trực tiếp (chỉ dành cho Admin, không qua thanh toán)
        /// </summary>
        public async Task<AccountSubscriptionResponse> CreateSubscriptionDirectlyAsync(Guid accountId, SubscribePackageRequest request)
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

            // 3. Kiểm tra account đã có subscription active chưa
            var existingActive = await GetAndRefreshActiveSubscriptionAsync(accountId);
            if (existingActive != null)
            {
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
                PackageTitle = subscriptionWithNav.SubscriptionPackage?.Title ?? string.Empty,
                RequiresPayment = false // Admin tạo trực tiếp không cần thanh toán
            };
        }
    }
}

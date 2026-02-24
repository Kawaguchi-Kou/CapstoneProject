using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AdvertisementService : IAdvertisementService
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IAccountSubscriptionService _subscriptionService;
        private readonly IPOIRepository _poiRepository;

        public AdvertisementService(
            IAdvertisementRepository advertisementRepository,
            IAccountSubscriptionService subscriptionService,
            IPOIRepository poiRepository)
        {
            _advertisementRepository = advertisementRepository;
            _subscriptionService = subscriptionService;
            _poiRepository = poiRepository;
        }

        public async Task<Advertisement> CreateAdvertisementAsync(Guid accountId, CreateAdvertisementRequest request)
        {
            // 1. Kiểm tra subscription active (không cần check hạn mức vì chưa approve)
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
            if (subscription == null)
            {
                throw new InvalidOperationException(
                    "Không thể tạo quảng cáo. Bạn chưa có subscription active.");
            }

            // 2. Kiểm tra POI có tồn tại không
            var poi = await _poiRepository.GetByIdAsync(request.POIId);
            if (poi == null)
            {
                throw new KeyNotFoundException("POI not found");
            }

            // 3. Sử dụng subscription đã lấy ở trên để lấy PackageId

            // 4. Validate dates
            if (request.StartDate >= request.EndDate)
            {
                throw new ArgumentException("StartDate must be before EndDate");
            }

            // 5. Tạo advertisement
            var advertisement = new Advertisement
            {
                AccountId = accountId,
                PackageId = subscription.SubscriptionPackageId,
                POIId = request.POIId,
                Title = request.Title,
                VideoUrl = request.VideoUrl,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = AdStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _advertisementRepository.CreateAsync(advertisement);

            // 6. KHÔNG tăng AdsUsed ở đây vì ad mới tạo có status = PendingApproval
            // AdsUsed chỉ tăng khi ad được approve (status = Active)

            return created;
        }

        public async Task<Advertisement?> GetByIdAsync(Guid adId)
        {
            return await _advertisementRepository.GetByIdAsync(adId);
        }

        public async Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId)
        {
            return await _advertisementRepository.GetByAccountIdAsync(accountId);
        }

        public async Task<Advertisement> ApproveAdvertisementAsync(Guid adId)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId);
            if (advertisement == null)
                throw new KeyNotFoundException("Advertisement not found");

            // Chỉ approve nếu status là PendingApproval
            if (advertisement.Status != AdStatus.PendingApproval)
            {
                throw new InvalidOperationException($"Cannot approve advertisement with status: {advertisement.Status}");
            }

            // 1. Kiểm tra subscription còn hạn mức không
            var canCreate = await _subscriptionService.CanCreateAdvertisementAsync(advertisement.AccountId);
            if (!canCreate)
            {
                throw new InvalidOperationException(
                    "Không thể approve quảng cáo. Subscription đã hết hạn mức hoặc không active.");
            }

            // 2. Approve ad (chuyển status sang Active)
            advertisement.Status = AdStatus.Active;
            await _advertisementRepository.UpdateAsync(advertisement);

            // 3. Tăng AdsUsed của subscription (sẽ tự động expire nếu AdsUsed >= MaxAds)
            await _subscriptionService.IncrementAdsUsedAsync(advertisement.AccountId);

            return advertisement;
        }

        public async Task<Advertisement> RejectAdvertisementAsync(Guid adId, string? reason = null)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId);
            if (advertisement == null)
                throw new KeyNotFoundException("Advertisement not found");

            // Chỉ reject nếu status là PendingApproval
            if (advertisement.Status != AdStatus.PendingApproval)
            {
                throw new InvalidOperationException($"Cannot reject advertisement with status: {advertisement.Status}");
            }

            advertisement.Status = AdStatus.Rejected;
            await _advertisementRepository.UpdateAsync(advertisement);

            return advertisement;
        }
    }
}

using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

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
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
            if (subscription == null)
            {
                throw new InvalidOperationException(
                    "Không thể tạo quảng cáo. Bạn chưa có subscription active.");
            }

            var poi = await _poiRepository.GetByIdAsync(request.POIId);
            if (poi == null)
            {
                throw new KeyNotFoundException("POI not found");
            }

            if (request.StartDate >= request.EndDate)
            {
                throw new ArgumentException("StartDate must be before EndDate");
            }

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

            return await _advertisementRepository.CreateAsync(advertisement);
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

            if (advertisement.Status != AdStatus.PendingApproval)
            {
                throw new InvalidOperationException($"Cannot approve advertisement with status: {advertisement.Status}");
            }

            var canCreate = await _subscriptionService.CanCreateAdvertisementAsync(advertisement.AccountId);
            if (!canCreate)
            {
                throw new InvalidOperationException(
                    "Không thể approve quảng cáo. Subscription đã hết hạn mức hoặc không active.");
            }

            advertisement.Status = AdStatus.Active;
            await _advertisementRepository.UpdateAsync(advertisement);

            await _subscriptionService.IncrementAdsUsedAsync(advertisement.AccountId);

            return advertisement;
        }

        public async Task<Advertisement> RejectAdvertisementAsync(Guid adId, string? reason = null)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId);
            if (advertisement == null)
                throw new KeyNotFoundException("Advertisement not found");

            if (advertisement.Status != AdStatus.PendingApproval)
            {
                throw new InvalidOperationException($"Cannot reject advertisement with status: {advertisement.Status}");
            }

            advertisement.Status = AdStatus.Rejected;
            await _advertisementRepository.UpdateAsync(advertisement);

            return advertisement;
        }

        public async Task<PagedResultResponse<PendingAdvertisementAccountItemResponse>> GetPendingAdvertisementAccountsAsync(
            int page,
            int pageSize,
            string? search = null)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var skip = (page - 1) * pageSize;
            var totalItems = await _advertisementRepository.CountPendingAccountsAsync(search);
            var rows = await _advertisementRepository.GetPendingAccountsAsync(skip, pageSize, search);

            var items = rows.Select(x => new PendingAdvertisementAccountItemResponse
            {
                AccountId = x.Account.Id,
                Email = x.Account.Email,
                Name = x.Account.Name,
                AvatarUrl = x.Account.AvatarUrl,
                PendingAdsCount = x.PendingAdsCount,
                LatestPendingAt = x.LatestPendingAt
            }).ToList();

            return new PagedResultResponse<PendingAdvertisementAccountItemResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        public async Task<PagedResultResponse<PendingAdvertisementItemResponse>> GetPendingAdvertisementsByAccountAsync(
            Guid accountId,
            int page,
            int pageSize,
            string? keyword = null)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var skip = (page - 1) * pageSize;
            var totalItems = await _advertisementRepository.CountPendingByAccountIdAsync(accountId, keyword);
            var ads = await _advertisementRepository.GetPendingByAccountIdAsync(accountId, skip, pageSize, keyword);

            var items = ads.Select(a => new PendingAdvertisementItemResponse
            {
                AdId = a.AdId,
                AccountId = a.AccountId,
                PackageId = a.PackageId,
                POIId = a.POIId,
                Title = a.Title,
                VideoUrl = a.VideoUrl,
                Content = a.Content,
                ImageUrl = a.ImageUrl,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                PoiName = a.POI?.Name ?? string.Empty
            }).ToList();

            return new PagedResultResponse<PendingAdvertisementItemResponse>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }
        public async Task<List<Advertisement>> GetAllAsync()
        {
            return await _advertisementRepository.GetAllAsync();
        }

        public async Task<List<Advertisement>> GetPendingAsync()
        {
            return await _advertisementRepository.GetPendingAsync();
        }

        public async Task<Advertisement> ApproveAdAsync(Guid adId)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId);

            if (advertisement == null)
                throw new KeyNotFoundException("Advertisement not found");

            if (advertisement.Status != AdStatus.PendingApproval)
            {
                throw new InvalidOperationException(
                    $"Cannot approve advertisement with status: {advertisement.Status}");
            }

            advertisement.Status = AdStatus.Active;

            return await _advertisementRepository.UpdateAsync(advertisement);
        }
    }
}

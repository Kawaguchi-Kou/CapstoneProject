using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    public class AdvertisementService : IAdvertisementService
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IAccountSubscriptionService _subscriptionService;
        private readonly IPOIRepository _poiRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public AdvertisementService(
            IAdvertisementRepository advertisementRepository,
            IAccountSubscriptionService subscriptionService,
            IPOIRepository poiRepository,
            ICloudinaryService cloudinaryService)
        {
            _advertisementRepository = advertisementRepository;
            _subscriptionService = subscriptionService;
            _poiRepository = poiRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<Advertisement> CreateAdvertisementAsync(Guid accountId, CreateAdvertisementRequest request)
        {
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
            if (subscription == null)
            {
                throw new InvalidOperationException("Không thể tạo quảng cáo. Bạn chưa có package/subscription còn hiệu lực.");
            }

            var canCreate = await _subscriptionService.CanCreateAdvertisementAsync(accountId);
            if (!canCreate)
            {
                throw new InvalidOperationException("Không thể tạo quảng cáo. Gói đã hết quota hoặc không còn hiệu lực.");
            }

            var poi = await _poiRepository.GetByIdAsync(request.POIId);
            
            if (poi == null)
            {
                throw new KeyNotFoundException("POI not found");
            }

            if (poi.Status != POIStatus.Active)
            {
                throw new InvalidOperationException("Chỉ có thể tạo quảng cáo trên POI đang Active.");
            }

            if (poi.PartnerId == null)
            {
                throw new InvalidOperationException("Partner chỉ được tạo quảng cáo trên POI của chính mình.");
            }

            if (poi.PartnerId != accountId)
            {
                throw new InvalidOperationException("Bạn không có quyền tạo quảng cáo trên POI này.");
            }

            if (request.StartDate >= request.EndDate)
            {
                throw new ArgumentException("StartDate must be before EndDate");
            }

            if (request.Promotion == null)
            {
                throw new ArgumentException("Promotion payload is required");
            }

            if (request.ImageFile != null && request.ImageFile.Length > 50 * 1024 * 1024)
            {
                throw new ArgumentException("Image file size must not exceed 50MB");
            }

            if (request.VideoFile != null && request.VideoFile.Length > 50 * 1024 * 1024)
            {
                throw new ArgumentException("Video file size must not exceed 50MB");
            }

            var startDateUtc = EnsureUtc(request.StartDate);
            var endDateUtc = EnsureUtc(request.EndDate);

            string? imageUrl = null;
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                using var stream = request.ImageFile.OpenReadStream();
                imageUrl = await _cloudinaryService.UploadImageAsync(stream, request.ImageFile.FileName);
            }

            string? videoUrl = null;
            if (request.VideoFile != null && request.VideoFile.Length > 0)
            {
                using var stream = request.VideoFile.OpenReadStream();
                videoUrl = await _cloudinaryService.UploadFileAsync(stream, request.VideoFile.FileName);
            }

            var advertisement = new Advertisement
            {
                AccountId = accountId,
                PackageId = subscription.SubscriptionPackageId,
                POIId = request.POIId,
                Title = request.Title,
                VideoUrl = videoUrl ?? string.Empty,
                Content = request.Content,
                ImageUrl = imageUrl ?? string.Empty,
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                Status = AdStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow
            };

            var promotion = new Promotion
            {
                Title = request.Promotion.Title,
                Description = request.Promotion.Description,
                Terms = request.Promotion.Terms,
                Status = PromotionStatus.Pending,
                SaveCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            return await _advertisementRepository.CreateWithPromotionAsync(advertisement, promotion);
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
            if (advertisement.Promotion != null)
            {
                advertisement.Promotion.Status = PromotionStatus.Active;
                advertisement.Promotion.UpdatedAt = DateTime.UtcNow;
            }

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
            if (advertisement.Promotion != null)
            {
                advertisement.Promotion.Status = PromotionStatus.Rejected;
                advertisement.Promotion.UpdatedAt = DateTime.UtcNow;
            }

            await _advertisementRepository.UpdateAsync(advertisement);
            return advertisement;
        }

        public async Task SavePromotionAsync(Guid accountId, Guid promotionId)
        {
            var promotion = await _advertisementRepository.GetPromotionByIdAsync(promotionId);
            if (promotion == null)
            {
                throw new KeyNotFoundException("Promotion not found");
            }

            if (promotion.Status != PromotionStatus.Active)
            {
                throw new InvalidOperationException("Promotion is not active");
            }

            if (promotion.Advertisement == null)
            {
                throw new InvalidOperationException("Promotion is not linked to any advertisement");
            }

            if (promotion.Advertisement.Status != AdStatus.Active)
            {
                throw new InvalidOperationException("Advertisement is not active");
            }

            var now = DateTime.UtcNow;
            if (promotion.Advertisement.StartDate > now || promotion.Advertisement.EndDate < now)
            {
                throw new InvalidOperationException("Advertisement is out of effective time window");
            }

            var alreadySaved = await _advertisementRepository.IsPromotionSavedAsync(accountId, promotionId);
            if (alreadySaved)
            {
                throw new InvalidOperationException("Promotion already saved");
            }

            var savedPromotion = new SavedPromotion
            {
                SavedPromotionId = Guid.NewGuid(),
                PromotionId = promotionId,
                AccountId = accountId,
                SavedAt = now
            };

            await _advertisementRepository.SavePromotionAsync(savedPromotion);

            promotion.SaveCount += 1;
            promotion.UpdatedAt = now;

            await _advertisementRepository.SaveChangesAsync();
        }

        public async Task<List<SavedPromotion>> GetSavedPromotionsByAccountIdAsync(Guid accountId)
        {
            return await _advertisementRepository.GetSavedPromotionsByAccountIdAsync(accountId);
        }

        public async Task<PagedResultResponse<PendingAdvertisementAccountItemResponse>> GetManagerAccountsAsync(
            int page,
            int pageSize,
            string? keyword = null)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var skip = (page - 1) * pageSize;
            var totalItems = await _advertisementRepository.CountManagerAccountsAsync(keyword);
            var rows = await _advertisementRepository.GetManagerAccountsAsync(skip, pageSize, keyword);

            var items = rows.Select(x => new PendingAdvertisementAccountItemResponse
            {
                AccountId = x.AccountId,
                Email = x.Email,
                Name = x.Name,
                PendingAdsCount = x.PendingAdsCount
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

        public async Task<PagedResultResponse<PendingAdvertisementItemResponse>> GetManagerAdvertisementsByAccountAsync(
            Guid accountId,
            string? status,
            int page,
            int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            if (!Enum.TryParse<AdStatus>(status, true, out var adStatus))
            {
                adStatus = AdStatus.PendingApproval;
            }

            var skip = (page - 1) * pageSize;
            var totalItems = await _advertisementRepository.CountByAccountIdAndStatusAsync(accountId, adStatus);
            var ads = await _advertisementRepository.GetByAccountIdAndStatusAsync(accountId, adStatus, skip, pageSize);

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

        public async Task<List<Advertisement>> GetActiveAsync()
        {
            return await _advertisementRepository.GetActiveAsync();
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}

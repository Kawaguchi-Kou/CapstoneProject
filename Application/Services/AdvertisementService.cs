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
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly IPreferenceRepository _preferenceRepository;

        public AdvertisementService(
            IAdvertisementRepository advertisementRepository,
            IAccountSubscriptionService subscriptionService,
            IPOIRepository poiRepository,
            ICloudinaryService cloudinaryService,
            IRealtimeNotifier realtimeNotifier,
            IPreferenceRepository preferenceRepository)
        {
            _advertisementRepository = advertisementRepository;
            _subscriptionService = subscriptionService;
            _poiRepository = poiRepository;
            _cloudinaryService = cloudinaryService;
            _realtimeNotifier = realtimeNotifier;
            _preferenceRepository = preferenceRepository;
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
                LimitSaveCount = request.Promotion.LimitSaveCount,
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

            var now = DateTime.UtcNow;

            if (advertisement.StartDate > now)
            {
                advertisement.Status = AdStatus.Scheduled;
                if (advertisement.Promotion != null)
                {
                    advertisement.Promotion.Status = PromotionStatus.Pending;
                    advertisement.Promotion.UpdatedAt = now;
                }
            }
            else
            {
                advertisement.Status = AdStatus.Active;
                if (advertisement.Promotion != null)
                {
                    advertisement.Promotion.Status = PromotionStatus.Active;
                    advertisement.Promotion.UpdatedAt = now;
                }
            }

            await _advertisementRepository.UpdateAsync(advertisement);
            await _subscriptionService.IncrementAdsUsedAsync(advertisement.AccountId);

            await _realtimeNotifier.SendUserNotificationAsync(advertisement.AccountId, new { Type = "AD_UPDATED", AdId = advertisement.AdId, Status = advertisement.Status.ToString() });

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
            await _realtimeNotifier.SendUserNotificationAsync(advertisement.AccountId, new { Type = "AD_UPDATED", AdId = advertisement.AdId, Status = advertisement.Status.ToString() });
            return advertisement;
        }

        public async Task<Advertisement> InactivateMyAdvertisementAsync(Guid accountId, Guid adId)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId)
                ?? throw new KeyNotFoundException("Advertisement not found");

            if (advertisement.AccountId != accountId)
                throw new InvalidOperationException("Bạn không có quyền tắt quảng cáo này.");

            var now = DateTime.UtcNow;
            if (advertisement.StartDate > now || advertisement.EndDate < now)
                throw new InvalidOperationException("Chỉ có thể tắt quảng cáo trong thời gian còn hiệu lực.");

            if (advertisement.Status != AdStatus.Active)
                throw new InvalidOperationException("Chỉ quảng cáo Active mới có thể tắt.");

            advertisement.Status = AdStatus.Paused;
            if (advertisement.Promotion != null && advertisement.Promotion.Status == PromotionStatus.Active)
            {
                advertisement.Promotion.Status = PromotionStatus.Inactive;
                advertisement.Promotion.UpdatedAt = now;
            }

            await _advertisementRepository.UpdateAsync(advertisement);
            return advertisement;
        }

        public async Task<Advertisement> ActivateMyAdvertisementAsync(Guid accountId, Guid adId)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId)
                ?? throw new KeyNotFoundException("Advertisement not found");

            if (advertisement.AccountId != accountId)
                throw new InvalidOperationException("Bạn không có quyền mở quảng cáo này.");

            var now = DateTime.UtcNow;
            if (advertisement.StartDate > now || advertisement.EndDate < now)
                throw new InvalidOperationException("Chỉ có thể mở quảng cáo trong thời gian còn hiệu lực.");

            if (advertisement.Status != AdStatus.Paused)
                throw new InvalidOperationException("Chỉ quảng cáo đang tắt (Paused) mới có thể mở lại.");

            var poi = await _poiRepository.GetByIdAsync(advertisement.POIId)
                ?? throw new KeyNotFoundException("POI not found");

            if (poi.Status != POIStatus.Active)
                throw new InvalidOperationException("Không thể mở quảng cáo vì POI liên kết đang không hoạt động.");

            advertisement.Status = AdStatus.Active;
            if (advertisement.Promotion != null)
            {
                advertisement.Promotion.Status = PromotionStatus.Active;
                advertisement.Promotion.UpdatedAt = now;
            }

            await _advertisementRepository.UpdateAsync(advertisement);
            return advertisement;
        }

        public async Task<Advertisement> UpdateAdvertisementAsync(Guid accountId, Guid adId, UpdateAdvertisementRequest request)
        {
            var advertisement = await _advertisementRepository.GetByIdAsync(adId)
                ?? throw new KeyNotFoundException("Advertisement not found");

            if (advertisement.AccountId != accountId)
                throw new InvalidOperationException("Bạn không có quyền cập nhật quảng cáo này.");

            bool isChanged = false;

            // Check and update Dates
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                if (request.StartDate.Value >= request.EndDate.Value)
                    throw new ArgumentException("StartDate must be before EndDate");
            }

            if (request.StartDate.HasValue)
            {
                var newStartDate = EnsureUtc(request.StartDate.Value);
                if (advertisement.StartDate != newStartDate)
                {
                    advertisement.StartDate = newStartDate;
                    isChanged = true;
                }
            }

            if (request.EndDate.HasValue)
            {
                var newEndDate = EnsureUtc(request.EndDate.Value);
                if (advertisement.EndDate != newEndDate)
                {
                    advertisement.EndDate = newEndDate;
                    isChanged = true;
                }
            }

            // Update files if provided
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                if (request.ImageFile.Length > 50 * 1024 * 1024)
                    throw new ArgumentException("Image file size must not exceed 50MB");

                using var stream = request.ImageFile.OpenReadStream();
                advertisement.ImageUrl = await _cloudinaryService.UploadImageAsync(stream, request.ImageFile.FileName);
                isChanged = true;
            }

            if (request.VideoFile != null && request.VideoFile.Length > 0)
            {
                if (request.VideoFile.Length > 50 * 1024 * 1024)
                    throw new ArgumentException("Video file size must not exceed 50MB");

                using var stream = request.VideoFile.OpenReadStream();
                advertisement.VideoUrl = await _cloudinaryService.UploadFileAsync(stream, request.VideoFile.FileName);
                isChanged = true;
            }

            // Update Advertisement fields
            if (!string.IsNullOrWhiteSpace(request.Title) && advertisement.Title != request.Title)
            {
                advertisement.Title = request.Title;
                isChanged = true;
            }

            if (request.Content != null && advertisement.Content != request.Content)
            {
                advertisement.Content = request.Content;
                isChanged = true;
            }

            // Update Promotion fields
            if (request.Promotion != null && advertisement.Promotion != null)
            {
                bool promoChanged = false;
                if (!string.IsNullOrWhiteSpace(request.Promotion.Title) && advertisement.Promotion.Title != request.Promotion.Title)
                {
                    advertisement.Promotion.Title = request.Promotion.Title;
                    promoChanged = true;
                }
                if (request.Promotion.Description != null && advertisement.Promotion.Description != request.Promotion.Description)
                {
                    advertisement.Promotion.Description = request.Promotion.Description;
                    promoChanged = true;
                }
                if (request.Promotion.Terms != null && advertisement.Promotion.Terms != request.Promotion.Terms)
                {
                    advertisement.Promotion.Terms = request.Promotion.Terms;
                    promoChanged = true;
                }
                if (request.Promotion.LimitSaveCount.HasValue && advertisement.Promotion.LimitSaveCount != request.Promotion.LimitSaveCount.Value)
                {
                    advertisement.Promotion.LimitSaveCount = request.Promotion.LimitSaveCount.Value;
                    promoChanged = true;
                }

                if (promoChanged)
                {
                    advertisement.Promotion.UpdatedAt = DateTime.UtcNow;
                    isChanged = true;
                }
            }

            // Reset status for re-approval ONLY if something actually changed
            // and the current status is Active or Rejected
            if (isChanged && (advertisement.Status == AdStatus.Active || advertisement.Status == AdStatus.Rejected))
            {
                advertisement.Status = AdStatus.PendingApproval;
                if (advertisement.Promotion != null)
                {
                    advertisement.Promotion.Status = PromotionStatus.Pending;
                }
            }

            await _advertisementRepository.UpdateAsync(advertisement);
            await _realtimeNotifier.SendUserNotificationAsync(advertisement.AccountId, new { Type = "AD_UPDATED", AdId = advertisement.AdId, Status = advertisement.Status.ToString() });
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

            if (promotion.LimitSaveCount > 0 && promotion.SaveCount >= promotion.LimitSaveCount)
            {
                throw new InvalidOperationException("Khuyến mãi đã đạt giới hạn lượt lưu.");
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

        public async Task<List<RecommendedAdsResponse>> GetActiveAsync(Guid? accountId = null)
        {
            var advertisements = await _advertisementRepository.GetActiveAsync();

            HashSet<Guid> userPrefSet = new HashSet<Guid>();
            if (accountId != null)
            {
                var userPreferenceIds = await _preferenceRepository.GetUserPreferenceIdsAsync(accountId.Value);
                userPrefSet = new HashSet<Guid>(userPreferenceIds);
            }

            // Calculate Jaccard Similarity and sort before mapping to DTO
            var scoredAds = advertisements.Select(ad =>
            {
                var poiPrefSet = new HashSet<Guid>(ad.POI?.PoiPreferences.Select(p => p.PreferenceId) ?? Enumerable.Empty<Guid>());
                double score = userPrefSet.Count > 0 ? CalculateJaccardSimilarity(userPrefSet, poiPrefSet) : 0;
                return new { Ad = ad, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Ad.CreatedAt)
            .ToList();

            return scoredAds.Select(x => new RecommendedAdsResponse
            {
                AdId = x.Ad.AdId,
                Title = x.Ad.Title,
                Content = x.Ad.Content,
                ImageUrl = x.Ad.ImageUrl,
                MatchScore = x.Score,
                MatchPercentage = (int)Math.Round(x.Score * 100),
                PoiName = x.Ad.POI?.Name ?? string.Empty,
                PartnerName = x.Ad.Account?.Name ?? string.Empty,
                PartnerAvatarUrl = x.Ad.Account?.AvatarUrl ?? string.Empty,
                Promotion = x.Ad.Promotion == null ? null : new RecommendedPromotionResponse
                {
                    PromotionId = x.Ad.Promotion.PromotionId,
                    Title = x.Ad.Promotion.Title,
                    Description = x.Ad.Promotion.Description,
                    SaveCount = x.Ad.Promotion.SaveCount,
                    LimitSaveCount = x.Ad.Promotion.LimitSaveCount
                }

            }).ToList();
        }

        private static double CalculateJaccardSimilarity(HashSet<Guid> set1, HashSet<Guid> set2)
        {
            if (set1.Count == 0 && set2.Count == 0) return 0;
            
            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();
            
            return union == 0 ? 0 : (double)intersection / union;
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

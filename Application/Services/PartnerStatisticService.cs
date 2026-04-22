using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PartnerStatisticService : IPartnerStatisticService
    {
        private readonly IPOIRepository _poiRepository;
        private readonly IAdvertisementRepository _advertisementRepository;

        public PartnerStatisticService(IPOIRepository poiRepository, IAdvertisementRepository advertisementRepository)
        {
            _poiRepository = poiRepository;
            _advertisementRepository = advertisementRepository;
        }

        public async Task<PartnerDashboardResponse> GetDashboardStatsAsync(Guid partnerId)
        {
            var pois = await _poiRepository.GetByPartnerIdAsync(partnerId);
            var ads = await _advertisementRepository.GetByAccountIdAsync(partnerId);

            var response = new PartnerDashboardResponse
            {
                // 1. Tổng số POI theo trạng thái
                PoiStatusStats = new PoiStatusStats
                {
                    Active = pois.Count(p => p.Status == POIStatus.Active),
                    Pending = pois.Count(p => p.Status == POIStatus.Pending),
                    Rejected = pois.Count(p => p.Status == POIStatus.Rejected),
                    Inactive = pois.Count(p => p.Status == POIStatus.Inactive)
                },

                // 2. Phân bổ theo loại hình
                PoiTypeStats = pois.GroupBy(p => p.Type)
                    .Select(g => new PoiTypeStats
                    {
                        Type = g.Key.ToString(),
                        Count = g.Count()
                    }).ToList(),

                // 3. Tổng lượt lưu ưu đãi (Save Count)
                TotalPromotionSaveCount = ads.Where(a => a.Promotion != null)
                    .Sum(a => a.Promotion!.SaveCount),

                // 4. Trạng thái quảng cáo
                AdStatusStats = new AdStatusStats
                {
                    Active = ads.Count(a => a.Status == AdStatus.Active),
                    PendingApproval = ads.Count(a => a.Status == AdStatus.PendingApproval),
                    Paused = ads.Count(a => a.Status == AdStatus.Paused),
                    Expired = ads.Count(a => a.Status == AdStatus.Expired),
                    Rejected = ads.Count(a => a.Status == AdStatus.Rejected)
                },

                // 5. Độ phủ theo địa điểm (Top interacted POIs by Save Count)
                TopInteractedPois = ads.Where(a => a.Promotion != null)
                    .GroupBy(a => a.POIId)
                    .Select(g => new PoiAdInteractionStats
                    {
                        PoiId = g.Key,
                        PoiName = g.First().POI?.Name ?? "N/A",
                        TotalSaveCount = g.Sum(a => a.Promotion!.SaveCount)
                    })
                    .OrderByDescending(x => x.TotalSaveCount)
                    .Take(5)
                    .ToList()
            };

            return response;
        }
    }
}

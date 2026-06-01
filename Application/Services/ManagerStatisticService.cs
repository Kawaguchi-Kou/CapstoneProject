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
    public class ManagerStatisticService : IManagerStatisticService
    {
        private readonly IPOIRepository _poiRepository;
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPartnerRequestRepository _partnerRequestRepository;

        public ManagerStatisticService(
            IPOIRepository poiRepository,
            IAdvertisementRepository advertisementRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            IPartnerRequestRepository partnerRequestRepository)
        {
            _poiRepository = poiRepository;
            _advertisementRepository = advertisementRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _partnerRequestRepository = partnerRequestRepository;
        }

        public async Task<ManagerDashboardResponse> GetManagerDashboardStatisticsAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            var pois = await _poiRepository.GetAllAsync();
            var ads = await _advertisementRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var payments = await _paymentRepository.GetAllAsync();
            var partnerRequests = await _partnerRequestRepository.GetAllAsync();

            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var start = startDate?.Date ?? end.AddDays(-7);

            var response = new ManagerDashboardResponse();

            // 1. Pending Counts (Only Partner POIs)
            var partnerPois = pois.Where(p => p.PartnerId != null).ToList();
            response.PendingPois = partnerPois.Count(p => p.Status == POIStatus.Pending);
            response.PendingAds = ads.Count(a => a.Status == AdStatus.PendingApproval);

            // 2. POI Approval Ratio (Only Partner POIs)
            var processedPois = partnerPois.Where(p => p.Status == POIStatus.Active || p.Status == POIStatus.Rejected).ToList();
            if (processedPois.Any())
            {
                response.PoiApprovalRatio.TotalProcessed = processedPois.Count;
                response.PoiApprovalRatio.ApprovedPercentage = Math.Round((double)processedPois.Count(p => p.Status == POIStatus.Active) / processedPois.Count * 100, 2);
                response.PoiApprovalRatio.RejectedPercentage = Math.Round((double)processedPois.Count(p => p.Status == POIStatus.Rejected) / processedPois.Count * 100, 2);
            }

            // 3. Ad Approval Ratio (All-time processed Ads)
            var processedAds = ads.Where(a => a.Status != AdStatus.PendingApproval).ToList();
            if (processedAds.Any())
            {
                // Active, Paused, Expired, Scheduled mean they were approved
                int approvedAds = processedAds.Count(a => a.Status == AdStatus.Active || a.Status == AdStatus.Paused || a.Status == AdStatus.Expired || a.Status == AdStatus.Scheduled);
                response.AdApprovalRatio.TotalProcessed = processedAds.Count;
                response.AdApprovalRatio.ApprovedPercentage = Math.Round((double)approvedAds / processedAds.Count * 100, 2);
                response.AdApprovalRatio.RejectedPercentage = Math.Round((double)processedAds.Count(a => a.Status == AdStatus.Rejected) / processedAds.Count * 100, 2);
            }

            // 4. Top POI Categories (All POIs - both Partner and System)
            var topCategories = pois.GroupBy(p => p.Type.ToString())
                                    .Select(g => new PoiCategoryStat
                                    {
                                        CategoryName = g.Key,
                                        Count = g.Count()
                                    })
                                    .OrderByDescending(c => c.Count)
                                    .Take(5)
                                    .ToList();
            
            int totalCategorized = topCategories.Sum(c => c.Count);
            foreach (var cat in topCategories)
            {
                cat.Percentage = totalCategorized > 0 ? Math.Round((double)cat.Count / totalCategorized * 100, 2) : 0;
            }
            response.TopPoiCategories = topCategories;

            // 5. Ad Status Breakdown
            response.AdStatusBreakdown.Active = ads.Count(a => a.Status == AdStatus.Active);
            response.AdStatusBreakdown.Paused = ads.Count(a => a.Status == AdStatus.Paused);
            response.AdStatusBreakdown.Expired = ads.Count(a => a.Status == AdStatus.Expired);
            response.AdStatusBreakdown.Rejected = ads.Count(a => a.Status == AdStatus.Rejected);

            // 6. New Partners Growth (Tính từ ngày yêu cầu đối tác được duyệt thành công)
            var approvedRequests = partnerRequests.Where(r => r.Status == PartnerRequestStatus.Approved 
                                                                && r.ReviewedAt.HasValue 
                                                                && r.ReviewedAt.Value.Date >= start 
                                                                && r.ReviewedAt.Value.Date <= end).ToList();
            
            if (period.ToLower() == "monthly")
            {
                var growthStats = approvedRequests.GroupBy(r => new { r.ReviewedAt!.Value.Year, r.ReviewedAt!.Value.Month })
                    .Select(g => new DailyPartnerGrowth { Date = $"{g.Key.Year}-{g.Key.Month:D2}", NewPartners = g.Count() }).ToList();

                var allMonths = new List<string>();
                var current = new DateTime(start.Year, start.Month, 1);
                var endMonth = new DateTime(end.Year, end.Month, 1);
                while (current <= endMonth)
                {
                    allMonths.Add(current.ToString("yyyy-MM"));
                    current = current.AddMonths(1);
                }

                response.NewPartnersGrowth = allMonths.Select(m => new DailyPartnerGrowth
                {
                    Date = m,
                    NewPartners = growthStats.FirstOrDefault(g => g.Date == m)?.NewPartners ?? 0
                }).ToList();
            }
            else
            {
                var growthStats = approvedRequests.GroupBy(r => r.ReviewedAt!.Value.Date)
                    .Select(g => new DailyPartnerGrowth { Date = g.Key.ToString("yyyy-MM-dd"), NewPartners = g.Count() }).ToList();

                var totalDays = (end - start).Days;
                var allDays = Enumerable.Range(0, totalDays + 1).Select(offset => start.AddDays(offset).ToString("yyyy-MM-dd")).ToList();
                
                response.NewPartnersGrowth = allDays.Select(d => new DailyPartnerGrowth
                {
                    Date = d,
                    NewPartners = growthStats.FirstOrDefault(g => g.Date == d)?.NewPartners ?? 0
                }).ToList();
            }

            // 7. Package Revenue
            // Only consider paid payments
            var paidPayments = payments.Where(p => p.PaymentStatus == PaymentStatus.Completed && p.PaidAt.HasValue && p.PaidAt.Value.Date >= start && p.PaidAt.Value.Date <= end).ToList();
            
            // Note: Since AdPayment does not directly link to Package cleanly without Subscription sometimes,
            // we should group by Subscription.SubscriptionPackage.Title if available.
            // If the structure is complex, we use PackageId if available, but AdPayment has PackageId?
            // Let's check AdPayment. It has PackageId. But getting Package Name requires IPackageRepository.
            // Since we loaded payments.Include(Subscription).ThenInclude(SubscriptionPackage), let's see.
            
            var revenueStats = paidPayments
                .Where(p => p.Subscription != null && p.Subscription.SubscriptionPackage != null)
                .GroupBy(p => p.Subscription.SubscriptionPackage.Title)
                .Select(g => new PackageRevenueStat
                {
                    PackageName = g.Key,
                    TotalRevenue = g.Sum(p => p.Amount)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();
                
            response.PackageRevenue = revenueStats;

            return response;
        }
    }
}

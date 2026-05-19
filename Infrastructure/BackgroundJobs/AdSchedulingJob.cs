using Domain.Enums;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs
{
    public class AdSchedulingJob : IAdSchedulingJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdSchedulingJob> _logger;

        public AdSchedulingJob(AppDbContext context, ILogger<AdSchedulingJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessScheduledAndExpiredAdsAsync()
        {
            var now = DateTime.UtcNow;

            // 1. Scheduled → Active (đã đến StartDate)
            var scheduledAds = await _context.Advertisements
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.Scheduled && a.StartDate <= now)
                .ToListAsync();

            foreach (var ad in scheduledAds)
            {
                ad.Status = AdStatus.Active;
                if (ad.Promotion != null)
                {
                    ad.Promotion.Status = PromotionStatus.Active;
                    ad.Promotion.UpdatedAt = now;
                }
            }

            // 2. Active → Expired (đã qua EndDate)
            var expiredAds = await _context.Advertisements
                .Include(a => a.Promotion)
                .Where(a => a.Status == AdStatus.Active && a.EndDate < now)
                .ToListAsync();

            foreach (var ad in expiredAds)
            {
                ad.Status = AdStatus.Expired;
                if (ad.Promotion != null
                    && ad.Promotion.Status == PromotionStatus.Active)
                {
                    ad.Promotion.Status = PromotionStatus.Inactive;
                    ad.Promotion.UpdatedAt = now;
                }
            }

            if (scheduledAds.Count > 0 || expiredAds.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Ad Scheduling: Activated {ActivatedCount}, Expired {ExpiredCount}",
                    scheduledAds.Count, expiredAds.Count);
            }
        }
    }
}

using Application.DTOs.AdminAnalytics;
using Domain.Entities;
using Infrastructure.EntitiesConfigurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        // ================= SUMMARY =================
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalUsers = await _context.Accounts.CountAsync();

            var activeUsers = await _context.Accounts
                .CountAsync(x => x.IsActive);

            var totalRevenue = await _context.adSubscriptionPackages
                .Where(x => x.Status.ToLower() == "active")
                .SumAsync(x => (decimal?)x.Price) ?? 0;

            return Ok(new AnalyticsSummaryDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRevenue = totalRevenue
            });
        }

        // ================= REVENUE CHART =================
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue()
        {
            var data = await _context.adSubscriptionPackages
                .Where(x => x.Status.ToLower() == "active")
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .Select(g => new RevenueChartDto
                {
                    Month = g.Key.Month.ToString(),
                    Revenue = g.Sum(x => x.Price)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Convert 1 → Jan
            foreach (var item in data)
            {
                int monthNumber = int.Parse(item.Month);
                item.Month = CultureInfo.CurrentCulture
                    .DateTimeFormat
                    .GetAbbreviatedMonthName(monthNumber);
            }

            return Ok(data);
        }

        // ================= ACCOUNT STATUS =================
        [HttpGet("accounts-status")]
        public async Task<IActionResult> GetAccountStatus()
        {
            var active = await _context.Accounts.CountAsync(x => x.IsActive);
            var inactive = await _context.Accounts.CountAsync(x => !x.IsActive);

            return Ok(new List<AccountStatusDto>
        {
            new() { Status = "Active", Count = active },
            new() { Status = "Inactive", Count = inactive }
        });
        }

        // ================= SUBSCRIPTION STATS =================
        [HttpGet("subscription-stats")]
        public async Task<IActionResult> GetSubscriptionStats()
        {
            var total = await _context.adSubscriptionPackages.CountAsync();
            var active = await _context.adSubscriptionPackages.CountAsync(x => x.Status.ToLower() == "active");
            var inactive = total - active;

            return Ok(new SubscriptionStatsDto
            {
                TotalPackages = total,
                ActivePackages = active,
                InactivePackages = inactive
            });
        }
    }
}

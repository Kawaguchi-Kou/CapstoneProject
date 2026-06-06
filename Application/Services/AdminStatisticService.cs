using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AdminStatisticService : IAdminStatisticService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountSubscriptionRepository _accountSubscriptionRepository;

        public AdminStatisticService(
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            IAccountSubscriptionRepository accountSubscriptionRepository)
        {
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _accountSubscriptionRepository = accountSubscriptionRepository;
        }

        public async Task<AdminDashboardResponse> GetDashboardStatisticsAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            var users = await _userRepository.GetAllAsync();
            var payments = await _paymentRepository.GetAllAsync();
            var subscriptions = await _accountSubscriptionRepository.GetAllAsync();

            // 1. Tổng số tài khoản
            var totalAccounts = users.Count;

            // 2. Breakdown by role
            var roleStats = new AccountRoleBreakdown
            {
                UserCount = users.Count(u => u.Role != null && u.Role.Name == "User"),
                PartnerCount = users.Count(u => u.Role != null && u.Role.Name == "Partner"),
                ManagerCount = users.Count(u => u.Role != null && u.Role.Name == "Manager"),
                StaffCount = users.Count(u => u.Role != null && u.Role.Name == "Staff")
            };

            // 3. Tốc độ tăng trưởng tài khoản
            var end = endDate?.Date ?? DateTime.UtcNow.Date;
            var start = startDate?.Date ?? end.AddDays(-7);
            
            var filteredUsers = users.Where(u => u.CreatedAt.Date >= start && u.CreatedAt.Date <= end).ToList();
            
            List<DailyAccountGrowth> completeGrowthStats = new List<DailyAccountGrowth>();

            if (!string.IsNullOrEmpty(period) && period.ToLower() == "monthly")
            {
                var growthStats = filteredUsers
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new DailyAccountGrowth
                    {
                        Date = $"{g.Key.Year}-{g.Key.Month:D2}",
                        NewAccounts = g.Count()
                    })
                    .ToList();

                var allMonths = new System.Collections.Generic.List<string>();
                var current = new DateTime(start.Year, start.Month, 1);
                var endMonth = new DateTime(end.Year, end.Month, 1);
                while (current <= endMonth)
                {
                    allMonths.Add(current.ToString("yyyy-MM"));
                    current = current.AddMonths(1);
                }

                completeGrowthStats = allMonths.Select(m => new DailyAccountGrowth
                {
                    Date = m,
                    NewAccounts = growthStats.FirstOrDefault(g => g.Date == m)?.NewAccounts ?? 0
                }).ToList();
            }
            else // daily
            {
                var growthStats = filteredUsers
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new DailyAccountGrowth
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        NewAccounts = g.Count()
                    })
                    .ToList();

                var totalDays = (end - start).Days;
                var allDays = Enumerable.Range(0, totalDays + 1).Select(offset => start.AddDays(offset).ToString("yyyy-MM-dd")).ToList();
                completeGrowthStats = allDays.Select(d => new DailyAccountGrowth
                {
                    Date = d,
                    NewAccounts = growthStats.FirstOrDefault(g => g.Date == d)?.NewAccounts ?? 0
                }).ToList();
            }


            // 4. Mức độ phổ biến của các gói (chỉ tính Active)
            var packageStats = subscriptions
                .Where(s => s.Status == Domain.Enums.SubStatus.Active && s.SubscriptionPackage != null)
                .GroupBy(s => s.SubscriptionPackage!.Title)
                .Select(g => new PackagePopularity
                {
                    PackageName = g.Key,
                    UserCount = g.Count()
                })
                .OrderByDescending(p => p.UserCount)
                .ToList();

            // 5. Tổng doanh thu (chỉ tính payment Completed) và số gói đang hoạt động
            var totalRevenue = payments
                .Where(p => p.PaymentStatus == Domain.Enums.PaymentStatus.Completed)
                .Sum(p => p.Amount);
            var activeSubscriptions = subscriptions
                .Count(s => s.Status == Domain.Enums.SubStatus.Active);

            return new AdminDashboardResponse
            {
                TotalAccounts = totalAccounts,
                TotalRevenue = totalRevenue,
                ActiveSubscriptions = activeSubscriptions,
                AccountRoles = roleStats,
                AccountGrowth = completeGrowthStats,
                PackagePopularity = packageStats
            };
        }
    }
}

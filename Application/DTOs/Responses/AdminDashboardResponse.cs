using System.Collections.Generic;

namespace Application.DTOs.Responses
{
    public class AdminDashboardResponse
    {
        public int TotalAccounts { get; set; }
        public int TotalPois { get; set; }
        public int TotalAds { get; set; }
        public AccountRoleBreakdown AccountRoles { get; set; } = new AccountRoleBreakdown();
        public List<DailyAccountGrowth> AccountGrowth { get; set; } = new List<DailyAccountGrowth>();
        public List<PackagePopularity> PackagePopularity { get; set; } = new List<PackagePopularity>();
    }

    public class AccountRoleBreakdown
    {
        public int UserCount { get; set; }
        public int PartnerCount { get; set; }
        public int ManagerCount { get; set; }
        public int StaffCount { get; set; }
    }

    public class DailyAccountGrowth
    {
        public string Date { get; set; } = string.Empty; // Format: yyyy-MM-dd
        public int NewAccounts { get; set; }
    }

    public class PackagePopularity
    {
        public string PackageName { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}

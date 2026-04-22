using System;
using System.Collections.Generic;

namespace Application.DTOs.Responses
{
    public class ManagerDashboardResponse
    {
        public int PendingPois { get; set; }
        public int PendingAds { get; set; }
        
        // Tỷ lệ phê duyệt / từ chối trong 30 ngày qua
        public ApprovalRatio PoiApprovalRatio { get; set; } = new ApprovalRatio();
        public ApprovalRatio AdApprovalRatio { get; set; } = new ApprovalRatio();

        public List<PoiCategoryStat> TopPoiCategories { get; set; } = new List<PoiCategoryStat>();
        public AdStatusBreakdown AdStatusBreakdown { get; set; } = new AdStatusBreakdown();
        
        public List<DailyPartnerGrowth> NewPartnersGrowth { get; set; } = new List<DailyPartnerGrowth>();
        public List<PackageRevenueStat> PackageRevenue { get; set; } = new List<PackageRevenueStat>();
    }

    public class ApprovalRatio
    {
        public int TotalProcessed { get; set; }
        public double ApprovedPercentage { get; set; }
        public double RejectedPercentage { get; set; }
    }

    public class PoiCategoryStat
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class AdStatusBreakdown
    {
        public int Active { get; set; }
        public int Paused { get; set; }
        public int Expired { get; set; }
        public int Rejected { get; set; }
    }

    public class DailyPartnerGrowth
    {
        public string Date { get; set; } = string.Empty;
        public int NewPartners { get; set; }
    }

    public class PackageRevenueStat
    {
        public string PackageName { get; set; } = string.Empty;
        public double TotalRevenue { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Application.DTOs.Responses
{
    public class PartnerDashboardResponse
    {
        public PoiStatusStats PoiStatusStats { get; set; } = new();
        public List<PoiTypeStats> PoiTypeStats { get; set; } = new();
        public int TotalPromotionSaveCount { get; set; }
        public AdStatusStats AdStatusStats { get; set; } = new();
        public List<PoiAdInteractionStats> TopInteractedPois { get; set; } = new();
    }

    public class PoiStatusStats
    {
        public int Active { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int Inactive { get; set; }
    }

    public class PoiTypeStats
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AdStatusStats
    {
        public int Active { get; set; }
        public int PendingApproval { get; set; }
        public int Paused { get; set; }
        public int Expired { get; set; }
        public int Rejected { get; set; }
    }

    public class PoiAdInteractionStats
    {
        public Guid PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public int TotalSaveCount { get; set; }
    }
}

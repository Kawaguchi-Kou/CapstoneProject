using System;

namespace Application.DTOs.Responses
{
    public class RecommendedAdsResponse
    {
        public Guid AdId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public int MatchPercentage { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAvatarUrl { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public RecommendedPromotionResponse? Promotion { get; set; }
    }

    public class RecommendedPromotionResponse
    {
        public Guid PromotionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SaveCount { get; set; }
        public int LimitSaveCount { get; set; }
    }
}


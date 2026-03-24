using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class AdvertisementResponse
    {
        public Guid AdId { get; set; }
        public Guid AccountId { get; set; }
        public Guid? PackageId { get; set; }
        public Guid POIId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AdStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public PromotionSummaryResponse? Promotion { get; set; }
    }

    public class PromotionSummaryResponse
    {
        public Guid PromotionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Terms { get; set; } = string.Empty;
        public PromotionStatus Status { get; set; }
        public int SaveCount { get; set; }
    }

    public class SavedPromotionResponse
    {
        public Guid SavedPromotionId { get; set; }
        public Guid PromotionId { get; set; }
        public Guid AdId { get; set; }
        public DateTime SavedAt { get; set; }
        public string PromotionTitle { get; set; } = string.Empty;
        public string AdvertisementTitle { get; set; } = string.Empty;
    }
}

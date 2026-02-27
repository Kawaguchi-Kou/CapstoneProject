using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class PendingAdvertisementItemResponse
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
        public string PoiName { get; set; } = string.Empty;
    }
}

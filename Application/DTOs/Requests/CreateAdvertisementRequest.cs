using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class CreateAdvertisementRequest
    {
        [Required]
        public Guid POIId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string VideoUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public CreatePromotionPayloadRequest Promotion { get; set; } = new();
    }
}

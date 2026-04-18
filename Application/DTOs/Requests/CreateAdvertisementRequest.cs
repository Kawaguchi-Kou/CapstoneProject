using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class CreateAdvertisementRequest
    {
        [Required]
        public Guid POIId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public IFormFile? VideoFile { get; set; }

        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public IFormFile? ImageFile { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public CreatePromotionPayloadRequest Promotion { get; set; } = new();
    }
}

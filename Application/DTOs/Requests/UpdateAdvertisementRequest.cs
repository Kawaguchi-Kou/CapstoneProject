using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class UpdateAdvertisementRequest
    {
        [MaxLength(100)]
        public string? Title { get; set; }

        public IFormFile? VideoFile { get; set; }

        [MaxLength(1000)]
        public string? Content { get; set; }

        public IFormFile? ImageFile { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public UpdatePromotionPayloadRequest? Promotion { get; set; }
    }

    public class UpdatePromotionPayloadRequest
    {
        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Terms { get; set; }
    }
}

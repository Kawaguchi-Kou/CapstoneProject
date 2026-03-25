using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class CreatePromotionPayloadRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Terms { get; set; } = string.Empty;
    }
}

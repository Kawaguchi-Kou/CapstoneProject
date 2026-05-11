using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class CreatePromotionPayloadRequest
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Terms { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "LimitSaveCount must be at least 0")]
        public int LimitSaveCount { get; set; }
    }
}


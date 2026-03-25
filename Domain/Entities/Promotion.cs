using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities
{
    public class Promotion
    {
        [Key]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid AdId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Terms { get; set; } = string.Empty;

        public PromotionStatus Status { get; set; } = PromotionStatus.Pending;

        public int SaveCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(AdId))]
        public Advertisement Advertisement { get; set; } = null!;

        public ICollection<SavedPromotion> SavedPromotions { get; set; } = new List<SavedPromotion>();
    }
}

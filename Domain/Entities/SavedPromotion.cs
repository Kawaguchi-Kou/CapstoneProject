using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class SavedPromotion
    {
        [Key]
        public Guid SavedPromotionId { get; set; }

        [Required]
        public Guid PromotionId { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        public DateTime SavedAt { get; set; }

        [ForeignKey(nameof(PromotionId))]
        public Promotion Promotion { get; set; } = null!;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
    }
}

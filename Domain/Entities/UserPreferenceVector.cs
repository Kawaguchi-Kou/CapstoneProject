using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class UserPreferenceVector
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        // dimension / feature
        [Required]
        [MaxLength(50)]
        public string PreferenceCode { get; set; } = string.Empty;
        // ví dụ: nature, food, budget, luxury, adventure

        // giá trị vector
        [Range(0, 1)]
        public double Score { get; set; }
    }
}

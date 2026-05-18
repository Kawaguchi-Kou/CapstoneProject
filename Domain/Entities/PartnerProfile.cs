using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PartnerProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [Required]
        [MaxLength(255)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BusinessAddress { get; set; } = string.Empty;

        [MaxLength(20)]
        public string BusinessPhone { get; set; } = string.Empty;

        [MaxLength(255)]
        [EmailAddress]
        public string BusinessEmail { get; set; } = string.Empty;

        public string BusinessLicenseUrl { get; set; } = string.Empty;

        public string BusinessAvatarUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation - quan hệ 1-1 với Account
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
    }
}

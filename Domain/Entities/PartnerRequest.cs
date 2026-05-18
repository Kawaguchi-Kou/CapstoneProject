using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class PartnerRequest
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

        public PartnerRequestStatus Status { get; set; } = PartnerRequestStatus.Pending;

        [MaxLength(1000)]
        public string AdminNote { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public Guid? ReviewedBy { get; set; }

        // Navigation
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
    }
}

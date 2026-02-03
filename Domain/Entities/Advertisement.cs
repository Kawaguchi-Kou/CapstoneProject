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
    public class Advertisement
    {
        [Key]
        public Guid AdId { get; set; }
        [Required]
        public Guid AccountId { get; set; }
        [Required]
        public Guid? PackageId { get; set; }
        [Required]
        public Guid POIId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public AdStatus Status { get; set; } = AdStatus.Draft;
        public DateTime CreatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
        [ForeignKey(nameof(PackageId))]
        public AdSubscriptionPackage? Package { get; set; }
        [ForeignKey(nameof(POIId))]
        public POI? POI { get; set; }
    }

}

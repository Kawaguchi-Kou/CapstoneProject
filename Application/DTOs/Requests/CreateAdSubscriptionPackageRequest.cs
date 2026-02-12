using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class CreateAdSubscriptionPackageRequest
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DurationDays must be greater than 0")]
        public int DurationDays { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "MaxAdsPerPeriod must be greater than 0")]
        public double MaxAdsPerPeriod { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(10)]
        public string? Currency { get; set; }
    }
}

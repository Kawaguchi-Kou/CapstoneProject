using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class CreateAdSubscriptionPackageRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public double MaxAdsPerPeriod { get; set; }
        public string? Status { get; set; }
        public string? Currency { get; set; }
    }
}

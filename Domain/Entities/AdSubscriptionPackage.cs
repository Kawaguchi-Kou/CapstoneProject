using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AdSubscriptionPackage
    {
        public Guid PackageId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public float Price { get; set; }
        public int DurationDays { get; set; }
        public string MaxAdsPerPeriod { get; set; } = string.Empty;

        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }

        public string Currency { get; set; } = "VND";

        // Navigation
        public ICollection<AccountSubscription> AccountSubscriptions { get; set; } = new List<AccountSubscription>();
    }

}

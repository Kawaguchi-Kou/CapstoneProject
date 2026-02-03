using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class AccountSubscription
    {
        public Guid SubscriptionId { get; set; }

        public Guid SubscriptionPackageId { get; set; }
        public Guid AccountId { get; set; }

        public float MaxAds { get; set; }
        public float AdsUsed { get; set; }

        public SubStatus Status { get; set; } = SubStatus.Active;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Account Account { get; set; } = null!;
        public AdSubscriptionPackage SubscriptionPackage { get; set; } = null!;
        public ICollection<AdPayment> Payments { get; set; } = new List<AdPayment>();
    }

}

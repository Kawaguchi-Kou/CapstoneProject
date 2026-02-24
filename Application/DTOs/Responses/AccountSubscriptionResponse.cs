using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class AccountSubscriptionResponse
    {
        public Guid SubscriptionId { get; set; }
        public Guid SubscriptionPackageId { get; set; }
        public Guid AccountId { get; set; }
        public int MaxAds { get; set; }
        public int AdsUsed { get; set; }
        public int AdsRemaining => MaxAds - AdsUsed;
        public SubStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PackageTitle { get; set; } = string.Empty;
    }
}

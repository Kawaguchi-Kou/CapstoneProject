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

        // Payment fields (khi cần thanh toán)
        public Guid? PaymentId { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? BankInfo { get; set; }
        public string? TransactionContent { get; set; }
        public bool RequiresPayment { get; set; } = false; // true nếu cần thanh toán trước
    }
}

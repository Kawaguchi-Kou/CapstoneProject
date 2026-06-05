using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public Guid PackageId { get; set; }
        public string PackageTitle { get; set; } = string.Empty;
        public float Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string TransactionContent { get; set; } = string.Empty;
        public DateTime? TransactionDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string BankInfo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public Guid? AccountId { get; set; }
        public string AccountEmail { get; set; } = string.Empty;
    }
}

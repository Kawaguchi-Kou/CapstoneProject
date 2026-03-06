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
        public Guid SubscriptionId { get; set; }
        public float Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string TransactionContent { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string BankInfo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

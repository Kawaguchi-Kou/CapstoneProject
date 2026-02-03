using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class AdPayment
    {
        public Guid PaymentId { get; set; }

        public Guid SubscriptionId { get; set; }

        public float Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime PaidAt { get; set; }

        // Navigation
        public AccountSubscription Subscription { get; set; } = null!;
    }

}

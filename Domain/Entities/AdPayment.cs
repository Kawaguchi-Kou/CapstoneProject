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

        // SubscriptionId phải nullable vì lúc tạo payment Pending thì chưa có subscription
        public Guid? SubscriptionId { get; set; }
        public Guid PackageId { get; set; } // PackageId để tạo subscription sau khi payment thành công
        public Guid AccountId { get; set; } // AccountId để tạo subscription

        public float Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime PaidAt { get; set; }

        // SePay specific fields
        public string TransactionContent { get; set; } = string.Empty; // Mã nội dung chuyển khoản (Pay{Guid}ment)
        public DateTime? TransactionDate { get; set; } // Ngày giao dịch từ SePay webhook
        public string? AccountNumber { get; set; } // Số tài khoản người chuyển
        public string? SubAccount { get; set; } // Sub account từ SePay
        public float? AmountIn { get; set; } // Số tiền nhận được
        public float? Accumulated { get; set; } // Số dư tích lũy
        public string? Gateway { get; set; } // Gateway từ SePay
        public string? Code { get; set; } // Mã giao dịch từ SePay

        // Navigation
        public AccountSubscription? Subscription { get; set; }
    }

}

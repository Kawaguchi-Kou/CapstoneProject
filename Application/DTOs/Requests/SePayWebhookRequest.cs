using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class SePayWebhookRequest
    {
        public string? TransferType { get; set; } // "in" hoặc "out"
        public string? Content { get; set; } // Nội dung chuyển khoản (chứa mã giao dịch)
        public string? TransactionDate { get; set; } // Ngày giao dịch
        public string? AccountNumber { get; set; } // Số tài khoản người chuyển
        public string? SubAccount { get; set; } // Sub account
        public float? TransferAmount { get; set; } // Số tiền chuyển
        public float? Accumulated { get; set; } // Số dư tích lũy
        public string? Gateway { get; set; } // Gateway
        public string? Code { get; set; } // Mã giao dịch
    }
}

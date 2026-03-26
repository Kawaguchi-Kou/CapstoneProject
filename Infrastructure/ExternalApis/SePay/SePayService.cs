using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalApis.SePay
{
    public class SePayService : ISePayService
    {
        private readonly SePayOptions _options;
        private readonly IConfiguration _config;

        public SePayService(IOptions<SePayOptions> options, IConfiguration configuration)
        {
            _options = options.Value;
            _config = configuration;
        }

        /// <summary>
        /// Tạo QR code URL cho SePay
        /// </summary>
        /// <param name="amount">Số tiền cần thanh toán</param>
        /// <param name="transactionContent">Nội dung chuyển khoản (mã giao dịch)</param>
        /// <returns>URL QR code SePay</returns>
        public string GenerateQrCodeUrl(float amount, string transactionContent)
        {
            // URL format: https://qr.sepay.vn/img?acc={accountNumber}&bank={bankName}&amount={amount}&des={description}
            var url = $"{_options.QrCodeBaseUrl}?acc={_options.AccountNumber}&bank={_options.BankName}&amount={amount}&des={transactionContent}";
            return url;
        }

        public string GetBankInfo()
        {
            return $"{_options.BankName} - {_options.AccountNumber}";
        }

        /// <summary>
        /// Verify API Key từ webhook header
        /// </summary>
        public bool VerifyApiKey(string apiKeyHeader)
        {
            var apiKeySepay = _config["API_KEY_SEPAY"];
            if (string.IsNullOrWhiteSpace(apiKeySepay))
            {
                return false;
            }

            // Bỏ prefix "Apikey " nếu có
            var normalized = apiKeyHeader?.Trim() ?? string.Empty;
            if (normalized.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("Apikey ".Length).Trim();
            }

            return normalized == apiKeySepay;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISePayService
    {
        string GenerateQrCodeUrl(float amount, string transactionContent);
        string GetBankInfo();
        bool VerifyApiKey(string apiKeyHeader);
    }
}

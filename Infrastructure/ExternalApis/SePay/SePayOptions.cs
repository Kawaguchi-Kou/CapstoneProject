namespace Infrastructure.ExternalApis.SePay
{
    public class SePayOptions
    {
        public const string SectionName = "SePay";

        public string ApiKey { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty; // Số tài khoản SePay (ví dụ: 0888294028)
        public string BankName { get; set; } = string.Empty; // Tên ngân hàng (ví dụ: VPBank)
        public string QrCodeBaseUrl { get; set; } = "https://qr.sepay.vn/img"; // Base URL cho QR code
    }
}

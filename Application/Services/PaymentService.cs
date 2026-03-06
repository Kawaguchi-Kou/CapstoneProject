using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
// Removed: using Infrastructure.ExternalApis.SePay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountSubscriptionRepository _subscriptionRepository;
        private readonly IAdSubscriptionPackageRepository _packageRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ISePayService _sePayService;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IAccountSubscriptionRepository subscriptionRepository,
            IAdSubscriptionPackageRepository packageRepository,
            IAuthRepository authRepository,
            ISePayService sePayService)
        {
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _packageRepository = packageRepository;
            _authRepository = authRepository;
            _sePayService = sePayService;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(Guid accountId, CreatePaymentRequest request)
        {
            // 1. Kiểm tra package có tồn tại không
            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null)
                throw new KeyNotFoundException("Package not found");

            // 2. Nếu Amount = 0 hoặc null, lấy từ package price
            float amount = request.Amount;
            if (amount == 0)
            {
                amount = (float)package.Price;
            }

            // 3. Validate amount với package price
            if (amount != (float)package.Price)
                throw new InvalidOperationException($"Amount mismatch. Expected: {package.Price}, Received: {amount}");

            // 3. Tạo payment ID và transaction content
            var paymentId = Guid.NewGuid();
            var transactionContent = $"Pay{paymentId}ment"; // Format: Pay{Guid}ment

            // 4. Tạo payment record với status Pending
            var payment = new AdPayment
            {
                PaymentId = paymentId,
                SubscriptionId = Guid.Empty, // Sẽ được cập nhật sau khi payment thành công
                PackageId = request.PackageId,
                AccountId = accountId,
                Amount = amount,
                Currency = package.Currency,
                PaymentMethod = "SePay",
                PaymentStatus = PaymentStatus.Pending,
                TransactionContent = transactionContent,
                PaidAt = DateTime.UtcNow
            };

            await _paymentRepository.CreateAsync(payment);

            // 5. Tạo QR code URL từ SePay
            var qrCodeUrl = _sePayService.GenerateQrCodeUrl(amount, transactionContent);

            // 6. Return response với QR code và thông tin thanh toán
            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                SubscriptionId = payment.SubscriptionId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.PaymentStatus,
                TransactionContent = payment.TransactionContent,
                QrCodeUrl = qrCodeUrl,
                BankInfo = "VP Bank - 0888294028 - SEPAY COMPANY",
                CreatedAt = payment.PaidAt
            };
        }

        public async Task<AdPayment?> ProcessSePayWebhookAsync(SePayWebhookRequest webhookRequest)
        {
            // 1. Chỉ xử lý giao dịch vào (incoming)
            if (webhookRequest.TransferType != "in")
                return null;

            // 2. Parse transaction content để lấy PaymentId
            if (string.IsNullOrEmpty(webhookRequest.Content))
                return null;

            var content = webhookRequest.Content;
            int startIndex = content.IndexOf("Pay") + "Pay".Length;
            int endIndex = content.IndexOf("ment");

            if (startIndex < 0 || endIndex <= startIndex)
                return null;

            string guidString = content.Substring(startIndex, endIndex - startIndex).Trim();
            if (!Guid.TryParse(guidString, out Guid paymentId))
                return null;

            // 3. Tìm payment record
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null || payment.PaymentStatus != PaymentStatus.Pending)
                return null;

            // 4. Cập nhật payment với thông tin từ webhook
            payment.PaymentStatus = PaymentStatus.Completed;
            if (DateTime.TryParse(webhookRequest.TransactionDate, out DateTime transactionDate))
                payment.TransactionDate = transactionDate;
            payment.AccountNumber = webhookRequest.AccountNumber;
            payment.SubAccount = webhookRequest.SubAccount;
            payment.AmountIn = webhookRequest.TransferAmount;
            payment.Accumulated = webhookRequest.Accumulated;
            payment.Gateway = webhookRequest.Gateway;
            payment.Code = webhookRequest.Code;
            payment.PaidAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            // 5. Nếu payment thành công và chưa có subscription, tạo subscription
            if (payment.PaymentStatus == PaymentStatus.Completed && payment.SubscriptionId == Guid.Empty)
            {
                try
                {
                    // Kiểm tra package
                    var package = await _packageRepository.GetByIdAsync(payment.PackageId);
                    if (package == null)
                        throw new KeyNotFoundException("Package not found");

                    // Kiểm tra account
                    var account = await _authRepository.GetByIdAsync(payment.AccountId);
                    if (account == null)
                        throw new KeyNotFoundException("Account not found");

                    // Kiểm tra account đã có subscription active chưa
                    var existingActive = await _subscriptionRepository.GetActiveByAccountIdAsync(payment.AccountId);
                    if (existingActive != null)
                    {
                        // Nếu đã có subscription active, link payment với subscription đó
                        payment.SubscriptionId = existingActive.SubscriptionId;
                        await _paymentRepository.UpdateAsync(payment);
                    }
                    else
                    {
                        // Tạo subscription mới
                        var subscription = new Domain.Entities.AccountSubscription
                        {
                            AccountId = payment.AccountId,
                            SubscriptionPackageId = payment.PackageId,
                            MaxAds = (int)package.MaxAdsPerPeriod,
                            AdsUsed = 0,
                            Status = Domain.Enums.SubStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };

                        var createdSubscription = await _subscriptionRepository.CreateAsync(subscription);
                        
                        // Cập nhật payment với subscriptionId
                        payment.SubscriptionId = createdSubscription.SubscriptionId;
                        await _paymentRepository.UpdateAsync(payment);
                    }
                }
                catch (Exception ex)
                {
                    // Log error nhưng không throw để webhook vẫn trả về success
                    // Payment đã được đánh dấu Completed, subscription sẽ được tạo thủ công sau
                    Console.WriteLine($"Error creating subscription after payment: {ex.Message}");
                }
            }

            return payment;
        }

        public async Task<AdPayment?> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _paymentRepository.GetByIdAsync(paymentId);
        }

        public async Task<List<AdPayment>> GetPaymentsBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _paymentRepository.GetBySubscriptionIdAsync(subscriptionId);
        }
    }
}

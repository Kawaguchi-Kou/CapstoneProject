using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
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
        private readonly IAccountSubscriptionService _subscriptionService;
        private readonly IAdSubscriptionPackageRepository _packageRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ISePayService _sePayService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IAccountSubscriptionRepository subscriptionRepository,
            IAccountSubscriptionService subscriptionService,
            IAdSubscriptionPackageRepository packageRepository,
            IAuthRepository authRepository,
            ISePayService sePayService,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _subscriptionService = subscriptionService;
            _packageRepository = packageRepository;
            _authRepository = authRepository;
            _sePayService = sePayService;
            _logger = logger;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(Guid accountId, CreatePaymentRequest request)
        {
            // 1. Kiểm tra package có tồn tại không
            var package = await _packageRepository.GetByIdAsync(request.PackageId);
            if (package == null)
                throw new KeyNotFoundException("Package not found");

            // 2. Kiểm tra account đã có subscription active chưa
            var activeSub = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
            if (activeSub != null)
            {
                throw new InvalidOperationException("Bạn hiện đang có một gói quảng cáo đang hoạt động. Vui lòng đợi gói hiện tại hết hạn hoặc sử dụng hết lượt quảng cáo trước khi đăng ký gói mới.");
            }

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
                SubscriptionId = null, // Pending: chưa có subscription, webhook sẽ gắn sau
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
                BankInfo = _sePayService.GetBankInfo(),
                CreatedAt = payment.PaidAt
            };
        }

        public async Task<AdPayment?> ProcessSePayWebhookAsync(SePayWebhookRequest webhookRequest)
        {
            _logger.LogInformation("🔔 SePay Webhook received - TransferType: {TransferType}, Content: {Content}", 
                webhookRequest.TransferType, webhookRequest.Content);

            // 1. Chỉ xử lý giao dịch vào (incoming)
            if (webhookRequest.TransferType != "in")
            {
                _logger.LogInformation("⏭️ Skipping outgoing transaction (TransferType: {TransferType})", webhookRequest.TransferType);
                return null;
            }

            // 2. Parse transaction content để lấy PaymentId
            if (string.IsNullOrEmpty(webhookRequest.Content))
            {
                _logger.LogWarning("⚠️ Webhook Content is null or empty");
                return null;
            }

            var content = webhookRequest.Content;
            int startIndex = content.IndexOf("Pay") + "Pay".Length;
            int endIndex = content.IndexOf("ment");

            if (startIndex < 0 || endIndex <= startIndex)
            {
                _logger.LogWarning("⚠️ Invalid transaction content format: {Content}", content);
                return null;
            }

            string guidString = content.Substring(startIndex, endIndex - startIndex).Trim();
            if (!Guid.TryParse(guidString, out Guid paymentId))
            {
                _logger.LogWarning("⚠️ Failed to parse PaymentId from content: {Content}, extracted: {GuidString}", content, guidString);
                return null;
            }

            _logger.LogInformation("🔍 Parsed PaymentId: {PaymentId}", paymentId);

            // 3. Tìm payment record
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("⚠️ Payment not found: {PaymentId}", paymentId);
                return null;
            }

            if (payment.PaymentStatus != PaymentStatus.Pending)
            {
                _logger.LogWarning("⚠️ Payment already processed: {PaymentId}, Status: {Status}", paymentId, payment.PaymentStatus);
                return null;
            }

            _logger.LogInformation("✅ Found pending payment: {PaymentId}", paymentId);

            // 4. Cập nhật payment với thông tin từ webhook
            _logger.LogInformation("💳 Updating payment status to Completed: {PaymentId}", paymentId);
            payment.PaymentStatus = PaymentStatus.Completed;
            if (DateTime.TryParse(webhookRequest.TransactionDate, out DateTime transactionDate))
            {
                // PostgreSQL timestamp with time zone yêu cầu UTC
                payment.TransactionDate = transactionDate.Kind switch
                {
                    DateTimeKind.Utc => transactionDate,
                    DateTimeKind.Local => transactionDate.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc)
                };
            }
            payment.AccountNumber = webhookRequest.AccountNumber;
            payment.SubAccount = webhookRequest.SubAccount;
            payment.AmountIn = webhookRequest.TransferAmount;
            payment.Accumulated = webhookRequest.Accumulated;
            payment.Gateway = webhookRequest.Gateway;
            payment.Code = webhookRequest.Code;
            payment.PaidAt = DateTime.UtcNow;

            await _paymentRepository.SaveChangesAsync();
            _logger.LogInformation("✅ Payment updated successfully: {PaymentId}", paymentId);

            // 5. Nếu payment thành công và chưa có subscription, tạo subscription
            if (payment.PaymentStatus == PaymentStatus.Completed && payment.SubscriptionId == null)
            {
                _logger.LogInformation("📦 Creating subscription for payment: {PaymentId}, AccountId: {AccountId}, PackageId: {PackageId}", 
                    paymentId, payment.AccountId, payment.PackageId);

                try
                {
                    // Kiểm tra package
                    var package = await _packageRepository.GetByIdAsync(payment.PackageId);
                    if (package == null)
                    {
                        _logger.LogError("❌ Package not found: {PackageId}", payment.PackageId);
                        throw new KeyNotFoundException($"Package not found: {payment.PackageId}");
                    }
                    _logger.LogInformation("✅ Package found: {PackageId}, Title: {Title}", package.PackageId, package.Title);

                    // Kiểm tra account
                    var account = await _authRepository.GetByIdAsync(payment.AccountId);
                    if (account == null)
                    {
                        _logger.LogError("❌ Account not found: {AccountId}", payment.AccountId);
                        throw new KeyNotFoundException($"Account not found: {payment.AccountId}");
                    }
                    _logger.LogInformation("✅ Account found: {AccountId}, Email: {Email}", account.Id, account.Email);

                    // Kiểm tra account đã có subscription active chưa
                    var existingActive = await _subscriptionRepository.GetActiveByAccountIdAsync(payment.AccountId);
                    if (existingActive != null)
                    {
                        _logger.LogInformation("🔗 Linking payment to existing subscription: {SubscriptionId}", existingActive.SubscriptionId);
                        // Nếu đã có subscription active, link payment với subscription đó
                        payment.SubscriptionId = existingActive.SubscriptionId;
                        await _paymentRepository.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogInformation("🆕 Creating new subscription for AccountId: {AccountId}", payment.AccountId);
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
                        _logger.LogInformation("✅ Subscription created successfully: {SubscriptionId}", createdSubscription.SubscriptionId);
                        
                        // Cập nhật payment với subscriptionId
                        payment.SubscriptionId = createdSubscription.SubscriptionId;
                        await _paymentRepository.SaveChangesAsync();
                        _logger.LogInformation("✅ Payment linked to subscription: {PaymentId} -> {SubscriptionId}", paymentId, createdSubscription.SubscriptionId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error chi tiết nhưng không throw để webhook vẫn trả về success
                    // Payment đã được đánh dấu Completed, subscription sẽ được tạo thủ công sau
                    _logger.LogError(ex, "❌ Error creating subscription after payment: {PaymentId}. Error: {Message}", paymentId, ex.Message);
                    _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                }
            }
            else
            {
                if (payment.SubscriptionId != null)
                {
                    _logger.LogInformation("ℹ️ Payment already has subscription: {PaymentId} -> {SubscriptionId}", paymentId, payment.SubscriptionId);
                }
            }

            _logger.LogInformation("✅ Webhook processing completed for payment: {PaymentId}", paymentId);
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

        public async Task<List<PaymentResponse>> GetPurchaseHistoryAsync(Guid accountId, string? userRole)
        {
            if (userRole != "Partner" && userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem lịch sử mua hàng. Yêu cầu role Partner.");
            }

            var payments = await _paymentRepository.GetByAccountIdAsync(accountId);
            
            // Lấy danh sách package titles để map nếu navigation property bị null
            var packageIds = payments.Select(p => p.PackageId).Distinct().ToList();
            var packageTitles = new Dictionary<Guid, string>();
            
            foreach (var packageId in packageIds)
            {
                var pkg = await _packageRepository.GetByIdAsync(packageId);
                if (pkg != null)
                {
                    packageTitles[packageId] = pkg.Title;
                }
            }

            return payments.Select(p => {
                var response = MapToPurchaseHistoryResponse(p);
                if (string.IsNullOrEmpty(response.PackageTitle) && packageTitles.TryGetValue(p.PackageId, out var title))
                {
                    response.PackageTitle = title;
                }
                return response;
            }).ToList();
        }

        private static PaymentResponse MapToPurchaseHistoryResponse(AdPayment payment)
        {
            return new PaymentResponse
            {
                PaymentId = payment.PaymentId,
                SubscriptionId = payment.SubscriptionId,
                PackageId = payment.PackageId,
                PackageTitle = payment.Subscription?.SubscriptionPackage?.Title ?? string.Empty,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.PaymentStatus,
                TransactionContent = payment.TransactionContent,
                TransactionDate = payment.TransactionDate,
                PaymentMethod = payment.PaymentMethod,
                QrCodeUrl = string.Empty,
                BankInfo = string.Empty,
                CreatedAt = payment.PaidAt
            };
        }
    }
}

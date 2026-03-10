using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IAuthService _authService;
        private readonly ISePayService _sePayService;

        public PaymentController(
            IPaymentService paymentService,
            IAuthService authService,
            ISePayService sePayService)
        {
            _paymentService = paymentService;
            _authService = authService;
            _sePayService = sePayService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                // Kiểm tra role Partner
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Partner")
                {
                    return StatusCode(403, new
                    {
                        message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Partner.",
                        role = userRole
                    });
                }

                // Lấy current user
                var currentUser = await _authService.GetCurrentAccount();
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "Invalid token: User not found" });
                }

                // Tạo payment và QR code
                var paymentResponse = await _paymentService.CreatePaymentAsync(currentUser.Id, request);

                // Hướng dẫn người dùng chuyển tiền
                return Ok(new
                {
                    message = "Vui lòng chuyển khoản theo hướng dẫn",
                    bank = paymentResponse.BankInfo,
                    paymentId = paymentResponse.PaymentId,
                    transactionContent = paymentResponse.TransactionContent,
                    amount = paymentResponse.Amount,
                    qrCodeUrl = paymentResponse.QrCodeUrl,
                    status = paymentResponse.Status.ToString()
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                // Tạm thời expose inner exception để debug lỗi DB
                return BadRequest(new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook(
            [FromBody] SePayWebhookRequest request,
            [FromHeader(Name = "Authorization")] string? apiKey)
        {
            // Log webhook received
            Console.WriteLine($"[WEBHOOK] Received at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"[WEBHOOK] TransferType: {request?.TransferType}");
            Console.WriteLine($"[WEBHOOK] Content: {request?.Content}");
            Console.WriteLine($"[WEBHOOK] API Key present: {!string.IsNullOrEmpty(apiKey)}");

            try
            {
                // Verify API Key
                if (string.IsNullOrEmpty(apiKey) || !_sePayService.VerifyApiKey(apiKey))
                {
                    Console.WriteLine("[WEBHOOK] ❌ Invalid API Key");
                    return Unauthorized(new { message = "Invalid API Key" });
                }

                Console.WriteLine("[WEBHOOK] ✅ API Key verified");

                // Chỉ xử lý giao dịch vào (incoming)
                if (request.TransferType != "in")
                {
                    Console.WriteLine($"[WEBHOOK] ⏭️ Ignoring outgoing transaction: {request.TransferType}");
                    return Ok(new { message = "Ignore: outgoing transaction" });
                }

                Console.WriteLine("[WEBHOOK] 🔄 Processing webhook...");

                // Process webhook
                var payment = await _paymentService.ProcessSePayWebhookAsync(request);

                if (payment == null)
                {
                    Console.WriteLine("[WEBHOOK] ⚠️ Payment not found or already processed");
                    return Ok(new { message = "Payment not found or already processed" });
                }

                Console.WriteLine($"[WEBHOOK] ✅ Successfully processed payment: {payment.PaymentId}, Status: {payment.PaymentStatus}");

                return Ok(new
                {
                    message = "Webhook processed successfully",
                    paymentId = payment.PaymentId,
                    status = payment.PaymentStatus.ToString(),
                    subscriptionId = payment.SubscriptionId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBHOOK] ❌ Exception: {ex.Message}");
                Console.WriteLine($"[WEBHOOK] StackTrace: {ex.StackTrace}");
                return BadRequest(new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                // Kiểm tra quyền truy cập (chỉ owner hoặc admin)
                var currentUser = await _authService.GetCurrentAccount();
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();

                if (payment.AccountId != currentUser.Id && userRole != "Admin")
                {
                    return Forbid("You don't have permission to view this payment");
                }

                var response = new PaymentResponse
                {
                    PaymentId = payment.PaymentId,
                    SubscriptionId = payment.SubscriptionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.PaymentStatus,
                    TransactionContent = payment.TransactionContent,
                    QrCodeUrl = _sePayService.GenerateQrCodeUrl(payment.Amount, payment.TransactionContent),
                    BankInfo = "MBBank - 0984147052 - NGUYEN HAI QUAN",
                    CreatedAt = payment.PaidAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("subscription/{subscriptionId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentsBySubscriptionId(Guid subscriptionId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsBySubscriptionIdAsync(subscriptionId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

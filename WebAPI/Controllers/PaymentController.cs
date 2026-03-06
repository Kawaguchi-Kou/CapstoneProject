using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            try
            {
                // Verify API Key
                if (string.IsNullOrEmpty(apiKey) || !_sePayService.VerifyApiKey(apiKey))
                {
                    return Unauthorized(new { message = "Invalid API Key" });
                }

                // Chỉ xử lý giao dịch vào (incoming)
                if (request.TransferType != "in")
                {
                    return Ok(new { message = "Ignore: outgoing transaction" });
                }

                // Process webhook
                var payment = await _paymentService.ProcessSePayWebhookAsync(request);

                if (payment == null)
                {
                    return Ok(new { message = "Payment not found or already processed" });
                }

                return Ok(new
                {
                    message = "Webhook processed successfully",
                    paymentId = payment.PaymentId,
                    status = payment.PaymentStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    BankInfo = "VP Bank - 0888294028 - SEPAY COMPANY",
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

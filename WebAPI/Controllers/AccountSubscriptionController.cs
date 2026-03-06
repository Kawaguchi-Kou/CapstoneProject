using System.Security.Claims;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/account-subscriptions")]
    [Authorize]
    public class AccountSubscriptionController : ControllerBase
    {
        private readonly IAccountSubscriptionService _subscriptionService;
        private readonly IAuthService _authService;

        public AccountSubscriptionController(
            IAccountSubscriptionService subscriptionService,
            IAuthService authService)
        {
            _subscriptionService = subscriptionService;
            _authService = authService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribePackage([FromBody] SubscribePackageRequest request)
        {
            try
            {
                // Kiểm tra role Partner
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Partner")
                {
                    return StatusCode(403, new { 
                        message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Partner.", 
                        role = userRole 
                    });
                }

                // Lấy accountId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                // ✅ LUỒNG CŨ: Tạo subscription trực tiếp (không qua thanh toán)
                // TODO: Khi có FE, có thể chuyển sang luồng thanh toán bằng cách:
                // 1. Gọi PaymentService.CreatePaymentAsync() thay vì SubscribePackageAsync()
                // 2. Trả về QR code và yêu cầu thanh toán
                // 3. Subscription sẽ được tạo tự động sau khi webhook SePay xác nhận thanh toán thành công
                var result = await _subscriptionService.SubscribePackageAsync(accountId, request);
                return Ok(result);
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

        [HttpGet("my-subscription")]
        public async Task<IActionResult> GetMySubscription()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
                if (subscription == null)
                {
                    return NotFound(new { message = "Bạn chưa có gói đăng ký nào" });
                }

                var response = new AccountSubscriptionResponse
                {
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionPackageId = subscription.SubscriptionPackageId,
                    AccountId = subscription.AccountId,
                    MaxAds = subscription.MaxAds,
                    AdsUsed = subscription.AdsUsed,
                    Status = subscription.Status,
                    CreatedAt = subscription.CreatedAt,
                    PackageTitle = subscription.SubscriptionPackage?.Title ?? string.Empty
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

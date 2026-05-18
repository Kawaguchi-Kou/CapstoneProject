using System.Security.Claims;
using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/partner-requests")]
    [Authorize]
    public class PartnerRequestController : ControllerBase
    {
        private readonly IPartnerRequestService _partnerRequestService;

        public PartnerRequestController(IPartnerRequestService partnerRequestService)
        {
            _partnerRequestService = partnerRequestService;
        }

        /// <summary>
        /// [User] Gửi đơn đăng ký trở thành Partner (hỗ trợ upload file giấy phép kinh doanh)
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRequest([FromForm] CreatePartnerRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerRequestService.CreateRequestAsync(accountId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        /// <summary>
        /// [User] Xem tình trạng đơn đăng ký mới nhất của mình
        /// </summary>
        [HttpGet("my-status")]
        public async Task<IActionResult> GetMyStatus()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerRequestService.GetMyLatestRequestAsync(accountId);
                if (result == null)
                    return Ok(new { message = "Bạn chưa gửi đơn đăng ký Partner nào." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        /// <summary>
        /// [Admin/Staff] Lấy danh sách đơn chờ duyệt (có phân trang)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetPendingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _partnerRequestService.GetPendingRequestsAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        /// <summary>
        /// [Admin/Staff] Duyệt hoặc Từ chối đơn đăng ký Partner
        /// </summary>
        [HttpPut("{id}/review")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ReviewRequest(Guid id, [FromBody] ReviewPartnerRequestDto request)
        {
            try
            {
                var reviewerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(reviewerIdClaim) || !Guid.TryParse(reviewerIdClaim, out var reviewerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerRequestService.ReviewRequestAsync(id, reviewerId, request);
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
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }
    }
}

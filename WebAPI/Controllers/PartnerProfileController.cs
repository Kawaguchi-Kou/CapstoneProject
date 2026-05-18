using System.Security.Claims;
using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/partner-profile")]
    [Authorize(Roles = "Partner")]
    public class PartnerProfileController : ControllerBase
    {
        private readonly IPartnerProfileService _partnerProfileService;

        public PartnerProfileController(IPartnerProfileService partnerProfileService)
        {
            _partnerProfileService = partnerProfileService;
        }

        /// <summary>
        /// [Partner] Lấy thông tin hồ sơ doanh nghiệp của mình
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerProfileService.GetMyProfileAsync(accountId);
                if (result == null)
                    return NotFound(new { message = "Hồ sơ Partner không tồn tại." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        /// <summary>
        /// [Partner] Cập nhật thông tin doanh nghiệp
        /// </summary>
        [HttpPut("my")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdatePartnerProfileDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerProfileService.UpdateMyProfileAsync(accountId, request);
                return Ok(result);
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
        /// [Partner] Cập nhật riêng Logo/Ảnh đại diện doanh nghiệp (upload file trực tiếp)
        /// </summary>
        [HttpPatch("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatarFile)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var result = await _partnerProfileService.UpdateAvatarAsync(accountId, avatarFile);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
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

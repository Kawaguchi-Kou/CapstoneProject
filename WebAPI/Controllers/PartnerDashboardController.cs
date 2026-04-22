using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/partner/dashboard")]
    [Authorize(Roles = "Partner")]
    public class PartnerDashboardController : ControllerBase
    {
        private readonly IPartnerStatisticService _statisticService;

        public PartnerDashboardController(IPartnerStatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var stats = await _statisticService.GetDashboardStatsAsync(partnerId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

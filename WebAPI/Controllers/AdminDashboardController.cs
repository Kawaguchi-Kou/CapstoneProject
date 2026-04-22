using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")] // Ensure only admins can access
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminStatisticService _statisticService;

        public AdminDashboardController(IAdminStatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetDashboardStatistics([FromQuery] string period = "daily", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var result = await _statisticService.GetDashboardStatisticsAsync(period, startDate, endDate);
            return Ok(result);
        }
    }
}

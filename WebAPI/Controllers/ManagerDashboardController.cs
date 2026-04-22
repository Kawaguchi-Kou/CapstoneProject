using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/manager/dashboard")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class ManagerDashboardController : ControllerBase
    {
        private readonly IManagerStatisticService _managerStatisticService;

        public ManagerDashboardController(IManagerStatisticService managerStatisticService)
        {
            _managerStatisticService = managerStatisticService;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetDashboardStatistics(
            [FromQuery] string period = "daily",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var stats = await _managerStatisticService.GetManagerDashboardStatisticsAsync(period, startDate, endDate);
            return Ok(stats);
        }
    }
}

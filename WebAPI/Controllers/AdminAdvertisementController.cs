using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/manager/advertisements")]
    [Authorize(Roles = "Manager")]
    public class ManagerAdvertisementController : ControllerBase
    {
        private readonly IAdvertisementService _advertisementService;

        public ManagerAdvertisementController(IAdvertisementService advertisementService)
        {
            _advertisementService = advertisementService;
        }

        [HttpGet("pending/accounts")]
        public async Task<IActionResult> GetPendingAccounts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _advertisementService.GetPendingAdvertisementAccountsAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingByAccount(
            [FromQuery] Guid accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (accountId == Guid.Empty)
                {
                    return BadRequest(new { message = "accountId is required" });
                }

                var result = await _advertisementService.GetPendingAdvertisementsByAccountAsync(accountId, page, pageSize, keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

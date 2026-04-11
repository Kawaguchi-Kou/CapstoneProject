using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/manager/accounts")]
    [Authorize(Roles = "Manager")]
    public class ManagerAdvertisementController : ControllerBase
    {
        private readonly IAdvertisementService _advertisementService;

        public ManagerAdvertisementController(IAdvertisementService advertisementService)
        {
            _advertisementService = advertisementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _advertisementService.GetManagerAccountsAsync(page, pageSize, keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{accountId}/advertisements")]
        public async Task<IActionResult> GetAdvertisementsByAccount(
            [FromRoute] Guid accountId,
            [FromQuery] string status = "PendingApproval",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (accountId == Guid.Empty)
                {
                    return BadRequest(new { message = "accountId is required" });
                }

                var result = await _advertisementService.GetManagerAdvertisementsByAccountAsync(accountId, status, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

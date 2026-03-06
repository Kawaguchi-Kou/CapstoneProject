using System.Security.Claims;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/pois")]
    public class POIController : ControllerBase
    {
        private readonly IPOIService _poiService;
        private readonly IAuthService _authService;

        public POIController(IPOIService poiService, IAuthService authService)
        {
            _poiService = poiService;
            _authService = authService;
        }

        [HttpGet("recommended")]
        [Authorize]
        public async Task<IActionResult> GetRecommendedPois(
            [FromQuery] int limit = 10)
        {
            //var accountId = Guid.Parse(
            //    User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            try
            {
                var account = _authService.GetCurrentAccount();
                var accountId = account.Result.Id;

                var result = await _poiService
                    .GetAllPoisSortedByPreferenceAsync(accountId);

                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}

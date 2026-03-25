using System.Security.Claims;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly IAdvertisementService _advertisementService;
        private readonly IMapper _mapper;

        public PromotionsController(IAdvertisementService advertisementService, IMapper mapper)
        {
            _advertisementService = advertisementService;
            _mapper = mapper;
        }

        [HttpPost("{promotionId}/save")]
        public async Task<IActionResult> SavePromotion(Guid promotionId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                await _advertisementService.SavePromotionAsync(accountId, promotionId);
                return Ok(new { message = "Promotion saved successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message == "Promotion already saved")
            {
                return Conflict(new { message = ex.Message });
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

        [HttpGet("/api/users/me/saved-promotions")]
        public async Task<IActionResult> GetMySavedPromotions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var savedPromotions = await _advertisementService.GetSavedPromotionsByAccountIdAsync(accountId);
                var response = _mapper.Map<List<SavedPromotionResponse>>(savedPromotions);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

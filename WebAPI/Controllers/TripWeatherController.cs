using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Manages on-demand OpenMeteo weather fetching for a trip.
    /// Two triggers:
    ///   1. POST /preload  — called by the frontend immediately after trip creation.
    ///   2. POST /refresh  — called when the user clicks "Update weather forecast" on the trip view.
    /// </summary>
    [ApiController]
    [Route("api/trip/{tripId:guid}/weather")]
    [Authorize]
    public class TripWeatherController : ControllerBase
    {
        private readonly IPlannerService _plannerService;

        public TripWeatherController(IPlannerService plannerService)
        {
            _plannerService = plannerService;
        }

        /// <summary>
        /// Fetches and caches fresh weather data for all segments of the trip.
        /// Call this once right after the trip (with base segments) has been created.
        /// </summary>
        /// <param name="tripId">ID of the newly created trip.</param>
        [HttpPost("preload")]
        public async Task<IActionResult> Preload(Guid tripId)
        {
            try
            {
                await _plannerService.PreloadTripWeatherAsync(tripId);
                return Ok(new { message = "Weather data preloaded successfully.", tripId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Forces a fresh fetch from OpenMeteo and overwrites cached weather rows for the trip.
        /// Call this when the user clicks "Get new weather forecast" / "Update weather forecast".
        /// </summary>
        /// <param name="tripId">ID of the trip to refresh weather for.</param>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(Guid tripId)
        {
            try
            {
                await _plannerService.PreloadTripWeatherAsync(tripId);
                return Ok(new { message = "Weather forecast refreshed successfully.", tripId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

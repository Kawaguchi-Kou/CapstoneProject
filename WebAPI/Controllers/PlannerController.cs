using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/planner")]
    public class PlannerController : ControllerBase
    {
        private readonly IPlannerService _plannerService;

        public PlannerController(IPlannerService plannerService)
        {
            _plannerService = plannerService;
        }

        /// <summary>
        /// Generate itinerary for a trip (AI Planner)
        /// </summary>
        [HttpPost("{tripId}/generate")]
        [Authorize]
        public async Task<IActionResult> Generate(Guid tripId)
        {
            try
            {
                await _plannerService.GenerateAsync(tripId);

                return Ok(new
                {
                    message = "Itinerary generated successfully",
                    tripId = tripId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    detail = ex.Message
                });
            }
        }

        ///// <summary>
        ///// Preview replan (AI suggestion only, not applied)
        ///// </summary>
        //[HttpPost("{tripId}/preview-replan")]
        //[Authorize]
        //public async Task<IActionResult> PreviewReplan(Guid tripId)
        //{
        //    try
        //    {
        //        var preview = await _plannerService.PreviewReplanAsync(tripId);

        //        return Ok(new
        //        {
        //            message = "Replan preview generated",
        //            data = preview
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            message = ex.Message
        //        });
        //    }
        //}

        ///// <summary>
        ///// Apply replan (user confirms changes)
        ///// </summary>
        //[HttpPost("{tripId}/apply-replan")]
        //[Authorize]
        //public async Task<IActionResult> ApplyReplan(Guid tripId)
        //{
        //    try
        //    {
        //        await _plannerService.ApplyReplanAsync(tripId);

        //        return Ok(new
        //        {
        //            message = "Trip updated successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            message = ex.Message
        //        });
        //    }
        //}

        ///// <summary>
        ///// Manual update itinerary detail (user override)
        ///// </summary>
        //[HttpPut("itinerary-detail/{detailId}")]
        //[Authorize]
        //public async Task<IActionResult> UpdateDetail(Guid detailId, [FromBody] UpdateItineraryDetailRequest request)
        //{
        //    try
        //    {
        //        await _plannerService.UpdateDetailAsync(detailId, request);

        //        return Ok(new
        //        {
        //            message = "Itinerary detail updated (manual override applied)"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            message = ex.Message
        //        });
        //    }
        //}

        ///// <summary>
        ///// Soft delete itinerary detail (user removes activity)
        ///// </summary>
        //[HttpDelete("itinerary-detail/{detailId}")]
        //[Authorize]
        //public async Task<IActionResult> DeleteDetail(Guid detailId)
        //{
        //    try
        //    {
        //        await _plannerService.DeleteDetailAsync(detailId);

        //        return Ok(new
        //        {
        //            message = "Itinerary detail removed"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new
        //        {
        //            message = ex.Message
        //        });
        //    }
        //}
    }
}
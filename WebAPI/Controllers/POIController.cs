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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var pois = await _poiService.GetAllAsync();
                return Ok(pois);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var poi = await _poiService.GetByIdAsync(id);

                if (poi == null)
                    return NotFound();

                return Ok(poi);
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePoiRequest request)
        {
            try
            {
                var id = await _poiService.CreateAsync(request);

                return Ok(id);
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePoiRequest request)
        {
            try
            {
                await _poiService.UpdateAsync(id, request);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _poiService.DeleteAsync(id);
                return NoContent();
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}

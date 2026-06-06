using System.Security.Claims;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly IDistrictService _districtService;

        public POIController(IPOIService poiService, IAuthService authService, IMapper mapper, IDistrictService districtService)
        {
            _poiService = poiService;
            _authService = authService;
            _mapper = mapper;
            _districtService = districtService;
        }

        [HttpGet("recommended")]
        [Authorize]
        public async Task<IActionResult> GetRecommendedPois(
            [FromQuery] int? limit = null)
        {
            try
            {
                var account = await _authService.GetCurrentAccount();
                var accountId = account.Id;

                var pois = await _poiService
                    .GetAllPoisSortedByPreferenceAsync(accountId);

                if (limit.HasValue)
                {
                    return Ok(pois.Take(limit.Value));
                }
                return Ok(pois);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("by-location/{locationId}")]
        public async Task<IActionResult> GetByLocation(Guid locationId)
        {
            var districts = await _districtService.GetByLocationIdAsync(locationId);

            return Ok(districts.Select(d => new
            {
                d.Id,
                d.Name
            }));
        }

        [HttpGet("grouped")]
        public async Task<IActionResult> GetAllGroupedPOIs()
        {
            var result = await _poiService
                .GetAllGroupedPOIsAsync();

            return Ok(result);
        }

    }

}

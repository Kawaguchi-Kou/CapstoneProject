using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/districts")]
    [Authorize(Roles = "Manager")]
    public class DistrictController : ControllerBase
    {
        private readonly IDistrictService _districtService;
        private readonly IMapper _mapper;

        public DistrictController(IDistrictService districtService, IMapper mapper)
        {
            _districtService = districtService;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var districts = await _districtService.GetAllAsync();
                var response = _mapper.Map<List<DistrictResponse>>(districts);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetByLocation([FromQuery] Guid locationId)
        {
            try
            {
                if (locationId == Guid.Empty)
                    return BadRequest(new { message = "locationId is required" });

                var districts = await _districtService.GetByLocationIdAsync(locationId);
                var response = _mapper.Map<List<DistrictResponse>>(districts);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDistrictRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Name is required" });

                if (request.LocationId == Guid.Empty)
                    return BadRequest(new { message = "LocationId is required" });

                var district = await _districtService.CreateAsync(request.Name, request.LocationId);
                var response = _mapper.Map<DistrictResponse>(district);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

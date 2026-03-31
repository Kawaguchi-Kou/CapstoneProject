using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/pois")]
    public class AdminController : ControllerBase
    {
        private readonly IPOIService _poiService;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        public AdminController(IPOIService poiService, IAuthService authService, IMapper mapper, ICloudinaryService cloudinaryService)
        {
            _poiService = poiService;
            _authService = authService;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var pois = await _poiService.GetAllAsync();

                var result = _mapper.Map<List<PoiResponse>>(pois);

                return Ok(result);
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

                var result = _mapper.Map<PoiResponse>(poi);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreatePoiRequest request, List<Guid> preferenceIds, POIType type)
        {
            try
            {
                string? POIImgUrl = null;

                try
                {
                    if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                    {
                        using var stream = request.POIImgUrl.OpenReadStream();
                        POIImgUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"File upload failed: {ex.Message}");
                }

                var poi = _mapper.Map<POI>(request);
                poi.POIImgUrl = POIImgUrl!;
                poi.Type = type;
                var newPoi = await _poiService.CreateAsync(poi, preferenceIds);

                var response = _mapper.Map<PoiResponse>(poi);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdatePoiRequest request)
        {
            try
            {
                string? POIImgUrl = null;

                try
                {
                    if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                    {
                        using var stream = request.POIImgUrl.OpenReadStream();
                        POIImgUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"File upload failed: {ex.Message}");
                }
                var poi = _mapper.Map<POI>(request);
                poi.POIImgUrl = POIImgUrl!;
                var updatedPOI = await _poiService.UpdateAsync(id, poi);
                var response = _mapper.Map<PoiResponse>(updatedPOI);

                return Ok(response);
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
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

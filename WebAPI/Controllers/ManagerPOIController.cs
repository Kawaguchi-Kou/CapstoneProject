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
    [Route("api/manager/pois")]
    [Authorize(Roles = "Manager")]
    public class ManagerPOIController : ControllerBase
    {
        private readonly IPOIService _poiService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public ManagerPOIController(IPOIService poiService, ICloudinaryService cloudinaryService, IMapper mapper)
        {
            _cloudinaryService = cloudinaryService;
            _poiService = poiService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var pois = await _poiService.GetAllAsync();
                var response = _mapper.Map<List<PoiResponse>>(pois);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var poi = await _poiService.GetByIdAsync(id);
            if (poi == null) return NotFound();
            return Ok(poi);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var pois = await _poiService.GetPendingPartnerPoisAsync(page, pageSize);

            var mapped = new PagedResultResponse<PoiResponse>
            {
                Items = _mapper.Map<List<PoiResponse>>(pois.Items),
                Page = pois.Page,
                PageSize = pois.PageSize,
                TotalItems = pois.TotalItems,
                TotalPages = pois.TotalPages
            };

            return Ok(mapped);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromForm] CreatePoiRequest request)
        {
            try
            {
                string? poiUrl = null;
                if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                {
                    using var stream = request.POIImgUrl.OpenReadStream();
                    poiUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                }

                var poi = _mapper.Map<POI>(request);
                poi.POIImgUrl = poiUrl;
                var response = await _poiService.CreateAsync(
                    poi,
                    request.PoiPreferences ?? new List<Guid>(),
                    request.LocationId,
                    request.DistrictId);
                var result = _mapper.Map<RecommendedPoiResponse>(response);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
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
                poi.POIImgUrl = POIImgUrl;
                var response = await _poiService.UpdateAsync(id, poi);
                return Ok(response);
            }catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            try
            {
                var poi = await _poiService.ApprovePartnerPoiAsync(id);
                var response = _mapper.Map<PoiResponse>(poi);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPoiRequest? request = null)
        {
            try
            {
                var poi = await _poiService.RejectPartnerPoiAsync(id);
                return Ok(new { poi, reason = request?.Reason });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/inactivate")]
        public async Task<IActionResult> Inactivate(Guid id, [FromQuery] bool confirmCascade = false)
        {
            try
            {
                var (poi, affectedAds) = await _poiService.InactivatePoiAsync(Guid.Empty, id, true, confirmCascade);
                return Ok(new
                {
                    poi,
                    affectedAds,
                    message = affectedAds > 0
                        ? $"POI đã inactive và {affectedAds} ads liên quan đã được inactive."
                        : "POI đã inactive thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                var poi = await _poiService.ActivatePoiAsync(id);
                return Ok(poi);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            await _poiService.ImportExcelAsync(file);
            return Ok("Import POI success");
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using var stream = file.OpenReadStream();
            var url = await _cloudinaryService.UploadImageAsync(stream, file.FileName);
            var key = Path.GetFileNameWithoutExtension(file.FileName).Trim().ToLower();
            _poiService.AddImageMapping(key, url);

            return Ok(new { url });
        }
    }
}

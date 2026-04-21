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
    [Route("api/partner/pois")]
    [Authorize(Roles = "Partner")]
    public class PartnerPOIController : ControllerBase
    {
        private readonly IPOIService _poiService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public PartnerPOIController(IPOIService poiService, ICloudinaryService cloudinaryService, IMapper mapper)
        {
            _poiService = poiService;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreatePoiRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                string? poiUrl = null;
                if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                {
                    using var stream = request.POIImgUrl.OpenReadStream();
                    poiUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                }

                var poi = _mapper.Map<POI>(request);
                poi.POIImgUrl = poiUrl;

                var response = await _poiService.CreatePartnerPoiAsync(
                    partnerId,
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

        [HttpGet("my")]
        public async Task<IActionResult> GetMyPois([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
            {
                return Unauthorized(new { message = "Invalid token: User ID not found" });
            }

            var pois = await _poiService.GetMyPoisAsync(partnerId, page, pageSize);

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

        [HttpGet("my/{id}")]
        public async Task<IActionResult> GetMyPoiById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
            {
                return Unauthorized(new { message = "Invalid token: User ID not found" });
            }

            var poi = await _poiService.GetMyPoiByIdAsync(partnerId, id);
            if (poi == null)
                return NotFound(new { message = "POI not found" });

            var mapped = _mapper.Map<PoiResponse>(poi);
            return Ok(mapped);
        }

        [HttpPut("my/{id}")]
        public async Task<IActionResult> UpdateMyPoi(Guid id, [FromForm] UpdatePoiRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var poi = _mapper.Map<POI>(request);

                // Upload ảnh mới nếu có
                if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                {
                    using var stream = request.POIImgUrl.OpenReadStream();
                    poi.POIImgUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                }

                var response = await _poiService.UpdatePartnerPoiAsync(partnerId, id, poi);
                var mapped = _mapper.Map<PoiResponse>(response);
                return Ok(mapped);
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

        [HttpPatch("my/{id}/inactivate")]
        public async Task<IActionResult> InactivateMyPoi(Guid id, [FromQuery] bool confirmCascade = false)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var (poi, affectedAds) = await _poiService.InactivatePoiAsync(partnerId, id, false, confirmCascade);
                var mapped = _mapper.Map<PoiResponse>(poi);

                return Ok(new
                {
                    poi = mapped,
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

        [HttpPost("my/{id}/request-reactivation")]
        public async Task<IActionResult> RequestReactivation(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var partnerId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var poi = await _poiService.RequestReactivationAsync(partnerId, id);
                var mapped = _mapper.Map<PoiResponse>(poi);

                return Ok(new
                {
                    poi = mapped,
                    message = "Đã gửi lại POI để Manager duyệt."
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
    }
}

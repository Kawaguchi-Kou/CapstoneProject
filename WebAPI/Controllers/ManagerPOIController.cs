using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

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
            var pois = await _poiService.GetAllAsync();
            return Ok(pois);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var poi = await _poiService.GetByIdAsync(id);

            if (poi == null)
                return NotFound();

            return Ok(poi);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePoiRequest request)
        {
            try
            {
                string? poiUrl = null;

                try
                {
                    if (request.POIImgUrl != null && request.POIImgUrl.Length > 0)
                    {
                        using var stream = request.POIImgUrl.OpenReadStream();
                        poiUrl = await _cloudinaryService.UploadImageAsync(stream, request.POIImgUrl.FileName);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"File upload failed: {ex.Message}");
                }
                var poi = _mapper.Map<POI>(request);
                poi.POIImgUrl = poiUrl;
                var response = await _poiService.CreateAsync(poi, request.PoiPreferences);
                var result = _mapper.Map<RecommendedPoiResponse>(response);
                return Ok(result);
            }   catch (Exception ex) 
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePoiRequest request)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _poiService.DeleteAsync(id);
            return Ok("Deleted successfully");
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
        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel()
        {
            var fileContent = await _poiService.ExportExcelAsync();
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "POIs_Export.xlsx");
        }
    }
}

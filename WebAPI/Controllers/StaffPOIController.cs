using Application.DTOs.Requests;
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
    [Route("api/staff/pois")]
    [Authorize(Roles = "Staff")]
    public class StaffPOIController : ControllerBase
    {
        private readonly IPOIService _poiService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public StaffPOIController(IPOIService poiService, ICloudinaryService cloudinaryService, IMapper mapper)
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
            var poi = _mapper.Map<POI>(request);
            var response = await _poiService.CreateAsync(poi);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePoiRequest request)
        {
            var poi = _mapper.Map<POI>(request);
            var response = await _poiService.UpdateAsync(id, poi);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _poiService.DeleteAsync(id);
            return Ok("Deleted successfully");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Staff")]
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

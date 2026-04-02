using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/manager/locations")]
    [Authorize(Roles = "Manager")]
    public class ManagerLocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public ManagerLocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var locations = await _locationService.GetAllAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var location = await _locationService.GetByIdAsync(id);

            if (location == null)
                return NotFound();

            return Ok(location);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateLocationRequest request)
        {
            var location = await _locationService.CreateAsync(request);
            return Ok(location);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateLocationRequest request)
        {
            var location = await _locationService.UpdateAsync(id, request);
            return Ok(location);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _locationService.DeleteAsync(id);
            return Ok("Deleted successfully");
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            await _locationService.ImportExcelAsync(file);

            return Ok("Import location success");
        }
        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel()
        {
            var fileContent = await _locationService.ExportExcelAsync();
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Locations_Export.xlsx");
        }
    }
}

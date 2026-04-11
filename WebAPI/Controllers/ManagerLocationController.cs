using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/manager/locations")]
    [Authorize(Roles = "Manager,Partner")]
    public class ManagerLocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public ManagerLocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var locations = await _locationService.GetAllAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var location = await _locationService.GetByIdAsync(id);

            if (location == null)
                return NotFound();

            return Ok(location);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(CreateLocationRequest request)
        {
            var location = await _locationService.CreateAsync(request);
            return Ok(location);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(Guid id, UpdateLocationRequest request)
        {
            var location = await _locationService.UpdateAsync(id, request);
            return Ok(location);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _locationService.DeleteAsync(id);
            return Ok("Deleted successfully");
        }

        [HttpPost("import")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            await _locationService.ImportExcelAsync(file);

            return Ok("Import location success");
        }
    }
}

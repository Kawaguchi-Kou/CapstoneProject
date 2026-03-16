using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/staff/locations")]
    [Authorize(Roles = "Staff")]
    public class StaffLocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public StaffLocationController(ILocationService locationService)
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
    }
}

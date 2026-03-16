using Application.DTOs.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/staff/pois")]
    [Authorize(Roles = "Staff")]
    public class StaffPOIController : ControllerBase
    {
        private readonly IPOIService _poiService;

        public StaffPOIController(IPOIService poiService)
        {
            _poiService = poiService;
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
            var poi = await _poiService.CreateAsync(request);
            return Ok(poi);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePoiRequest request)
        {
            var poi = await _poiService.UpdateAsync(id, request);
            return Ok(poi);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _poiService.DeleteAsync(id);
            return Ok("Deleted successfully");
        }
    }
}

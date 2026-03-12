using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
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

        public AdminController(IPOIService poiService, IAuthService authService, IMapper mapper)
        {
            _poiService = poiService;
            _authService = authService;
            _mapper = mapper;
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
        public async Task<IActionResult> Create(CreatePoiRequest request)
        {
            try
            {
                var poi = await _poiService.CreateAsync(request);

                var response = _mapper.Map<PoiResponse>(poi);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdatePoiRequest request)
        {
            try
            {
                var updatedPOI = await _poiService.UpdateAsync(id, request);
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

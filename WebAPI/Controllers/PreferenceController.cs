using System.ComponentModel.Design;
using System.Security.Claims;
using Application.Interfaces;
using Application.Services;
using Application.DTOs.Requests;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    public class PreferenceController : ControllerBase
    {
        private readonly IPreferenceService _preferenceService;

        public PreferenceController(IPreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var preferences = await _preferenceService.GetAllPreferencesAsync();

                return Ok(preferences);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] PreferenceRequest request)
        {
            try
            {
                var preference = await _preferenceService.CreatePreferenceAsync(request.Name);
                return Ok(preference);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PreferenceRequest request)
        {
            try
            {
                var preference = await _preferenceService.UpdatePreferenceAsync(id, request.Name);
                return Ok(preference);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _preferenceService.DeletePreferenceAsync(id);
                return Ok(new { message = "Preference deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

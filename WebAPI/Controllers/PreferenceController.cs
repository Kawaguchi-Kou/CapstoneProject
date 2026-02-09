using System.ComponentModel.Design;
using System.Security.Claims;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    public class PreferenceController : Controller
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
    }
}

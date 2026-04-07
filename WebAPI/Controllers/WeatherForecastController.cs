using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IOpenMeteoService _weatherService;

        public WeatherForecastController(IOpenMeteoService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            double latitude,
            double longitude,
            DateTime from,
            DateTime to)
        {
            try
            {
                var result = await _weatherService.GetDailyAsync(
                latitude,
                longitude,
                from,
                to);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
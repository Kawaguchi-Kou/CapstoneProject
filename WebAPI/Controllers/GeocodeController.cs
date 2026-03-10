using Application.DTOs.Geocode;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeocodeController : ControllerBase
    {
        private readonly IGeocodingService _geocoding;
        private readonly ILogger<GeocodeController> _logger;

        public GeocodeController(
            IGeocodingService geocoding,
            ILogger<GeocodeController> logger)
        {
            _geocoding = geocoding;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<GeocodeResponse>> Get(string placeName, string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placeName))
                    return BadRequest("placeName is required.");

                var (latitude, longitude) =
                    await _geocoding.GetCoordinatesAsync(placeName, city);

                var response = new GeocodeResponse
                {
                    PlaceName = placeName,
                    Latitude = latitude,
                    Longitude = longitude
                };

                return Ok(response);
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
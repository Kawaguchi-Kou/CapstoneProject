using Application.DTOs.Geocode;
using Application.DTOs.Tools;
using Application.DTOs.Weather;
using Application.Interfaces;
using Application.Tools;
using Infrastructure.ExternalApis.OpenMeteo;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("tools")]
public class ToolsController : ControllerBase
{
    private readonly IOpenMeteoService _weather;
    private readonly IGeocodingService _geocoding;

    public ToolsController(IOpenMeteoService weather, IGeocodingService geocoding)
    {
        _weather = weather;
        _geocoding = geocoding;
    }

    public class McpRequest<T>
    {
        public T Req { get; set; } = default!;
    }


    // MCP: list tools
    [HttpGet]
    public IActionResult ListTools()
    {
        return Ok(new
        {
            tools = new[]
            {
                McpToolSchemas.GetWeather,
                McpToolSchemas.GetCoordinates
            }
        });
    }

    [HttpPost("get_weather")]
    public async Task<IActionResult> GetWeather(
    [FromBody] WeatherRequest request)
    {
        var r = request;

        var result = await _weather.GetDailyAsync(
            r.Latitude,
            r.Longitude,
            r.StartDate,
            r.EndDate
        );

        return Ok(new { content = result });
    }

    [HttpPost("get_coordinates")]
    public async Task<IActionResult> GetCoordinates(
    [FromBody] McpRequest<GeocodeRequest> request)
    {
        var placeName = request.Req.PlaceName;
        var city = request.Req.City;

        if (string.IsNullOrWhiteSpace(placeName))
            return BadRequest("placeName is required");

        var (latitude, longitude) =
            await _geocoding.GetCoordinatesAsync(placeName, city);

        return Ok(new
        {
            content = new
            {
                latitude,
                longitude
            }
        });
    }
}


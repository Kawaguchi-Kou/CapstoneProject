using Application.DTOs.Tools;
using Application.DTOs.Weather;
using Application.Interfaces;
using Application.Tools;
using Infrastructure.ExternalApis.OpenMeteo;
using Microsoft.AspNetCore.Mvc;
//using Infrastructure.ExternalApis.GoogleMaps;

namespace WebAPI.Controllers;

[ApiController]
[Route("tools")]
public class ToolsController : ControllerBase
{
    private readonly IOpenMeteoService _weather;

    public ToolsController(IOpenMeteoService weather)
    {
        _weather = weather;
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
                McpToolSchemas.GetWeather
            }
        });
    }

    // MCP: weather (daily)
    [HttpPost("get_weather")]
    public async Task<IActionResult> GetWeather(
    [FromBody] McpRequest<WeatherRequest> request)
    {
        var r = request.Req;

        var result = await _weather.GetDailyAsync(
            r.Latitude,
            r.Longitude,
            r.StartDate,
            r.EndDate
        );

        return Ok(new { content = result });
    }
}


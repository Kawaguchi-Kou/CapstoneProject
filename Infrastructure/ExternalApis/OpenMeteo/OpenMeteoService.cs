using System.Text.Json;
using Application.DTOs.Weather;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalApis.OpenMeteo
{
    public class OpenMeteoService : IOpenMeteoService
    {
        private readonly HttpClient _http;
        private readonly OpenMeteoOptions _options;

        public OpenMeteoService(
            HttpClient http,
            IOptions<OpenMeteoOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<DailyWeatherDto>> GetDailyAsync(
            double latitude,
            double longitude,
            DateOnly from,
            DateOnly to)
        {
            var url =
                $"{_options.BaseUrl}" +
                $"?latitude={latitude}" +
                $"&longitude={longitude}" +
                $"&daily=temperature_2m_max,precipitation_probability_max,wind_speed_10m_max" +
                $"&start_date={from:yyyy-MM-dd}" +
                $"&end_date={to:yyyy-MM-dd}" +
                $"&timezone=auto";

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OpenMeteoDailyResponse>(json)!;

            var result = new List<DailyWeatherDto>();

            for (int i = 0; i < data.Daily.Time.Count; i++)
            {
                result.Add(new DailyWeatherDto
                {
                    Date = DateOnly.Parse(data.Daily.Time[i]),
                    MaxTemperature = data.Daily.TemperatureMax[i],
                    PrecipitationProbability = data.Daily.PrecipitationProbabilityMax[i],
                    MaxWindSpeed = data.Daily.WindSpeedMax[i]
                });
            }

            return result;
        }
    }
}

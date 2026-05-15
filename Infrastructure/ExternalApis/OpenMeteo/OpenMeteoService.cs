using System.Globalization;
using System.Text.Json;
using Application.DTOs.Weather;
using Application.Interfaces;
using CloudinaryDotNet;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalApis.OpenMeteo
{
    public class OpenMeteoService : IOpenMeteoService
    {
        private readonly HttpClient _http;
        private readonly OpenMeteoOptions _options;
        private readonly IWeatherForecastRepository _repo;
        private readonly ILocationRepository _locationRepo;
        private static readonly SemaphoreSlim _apiLimiter = new(2); // limit concurrency

        public OpenMeteoService(
            HttpClient http,
            IOptions<OpenMeteoOptions> options, IWeatherForecastRepository weatherForecastRepository, ILocationRepository locationRepo)
        {
            _http = http;
            _options = options.Value;
            _repo = weatherForecastRepository;
            _locationRepo = locationRepo;
        }

        public async Task<IReadOnlyList<DailyWeatherDto>> GetDailyAsync(
    double latitude,
    double longitude,
    DateTime from,
    DateTime to)
        {
            if (to < from)
                throw new ArgumentException("End date must be after start date.");

            var today = DateTime.Now;

            if (from < today)
                from = today;

            var days = (to.Date - from.Date).Days + 1;

            if (days <= 0)
                return new List<DailyWeatherDto>();

            if (days > 7)
                throw new ArgumentException("Forecast period cannot exceed 7 days.");

            var url =
                $"{_options.BaseUrl}" +
                $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&daily=temperature_2m_max,precipitation_probability_max,wind_speed_10m_max" +
                $"&start_date={from:yyyy-MM-dd}" +
                $"&end_date={to:yyyy-MM-dd}" +
                $"&timezone=auto";

            int retry = 0;

            while (retry < 3)
            {
                await _apiLimiter.WaitAsync();

                try
                {
                    var response = await _http.GetAsync(url);

                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new OpenMeteoApiException(response.StatusCode, json);

                    var data = JsonSerializer.Deserialize<OpenMeteoDailyResponse>(json)!;

                    var result = new List<DailyWeatherDto>();

                    for (int i = 0; i < data.Daily.Time.Count; i++)
                    {
                        result.Add(new DailyWeatherDto
                        {
                            Date = DateTime.Parse(data.Daily.Time[i]),
                            MaxTemperature = data.Daily.TemperatureMax[i],
                            PrecipitationProbability = data.Daily.PrecipitationProbabilityMax[i],
                            MaxWindSpeed = data.Daily.WindSpeedMax[i]
                        });
                    }

                    return result;
                }
                catch (HttpRequestException ex)
                {
                    retry++;
                    Console.WriteLine($"⚠️ Retry {retry}: {ex}");
                    await Task.Delay(500 * retry);
                }
                catch (TaskCanceledException ex)
                {
                    retry++;
                    Console.WriteLine($"⚠️ Timeout retry {retry}: {ex}");
                    await Task.Delay(500 * retry);
                }
                finally
                {
                    _apiLimiter.Release();
                }
            }

            // 🔥 IMPORTANT
            throw new Exception("OpenMeteo failed after retries");
        }

        public async Task<DailyWeatherDto?> GetSingleDayAsync(
            double latitude,
            double longtitude,
            DateTime date)
        {
            var list = await GetDailyAsync(
                latitude,
                longtitude,
                date,
                date);

            return list.FirstOrDefault();
        }

        public async Task<WeatherForecast> GetAsync(
        Guid locationId,
        DateTime date)
        {
            var cached = await _repo.GetAsync(locationId, date);

            if (cached != null)
                return cached;

            var loc = await _locationRepo.GetByIdAsync(locationId);

            var apiData = await GetDailyAsync(
                loc.Latitude,
                loc.Longitude,
                date,
                date);

            var dto = apiData.FirstOrDefault();

            var entity = new WeatherForecast
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                ForecastDate = date,
                TemperatureCelsius = dto.MaxTemperature,
                PrecipitationProbability = dto.PrecipitationProbability,
                WindSpeed = dto.MaxWindSpeed,
                FetchedAt = DateTime.UtcNow
            };

            await _repo.UpsertAsync(new List<WeatherForecast> { entity });

            return entity;
        }

        public async Task<List<WeatherForecast>> GetForecastRangeAsync(
    Guid locationId,
    List<DateTime> dates)
        {
            if (dates == null || !dates.Any())
                return new List<WeatherForecast>();

            dates = dates
                .Select(x => x.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var location = await _locationRepo.GetByIdAsync(locationId)
                ?? throw new Exception("Location not found");

            var from = dates.Min();
            var to = dates.Max();

            var apiData = await GetDailyAsync(
                location.Latitude,
                location.Longitude,
                from,
                to);

            return apiData
                .Where(x => dates.Contains(x.Date.Date))
                .Select(x => new WeatherForecast
                {
                    Id = Guid.NewGuid(),
                    LocationId = locationId,
                    City = location.LocationName,
                    ForecastDate = x.Date.Date,
                    TemperatureCelsius = x.MaxTemperature,
                    PrecipitationProbability = x.PrecipitationProbability,
                    WindSpeed = x.MaxWindSpeed,
                    FetchedAt = DateTime.UtcNow
                })
                .ToList();
        }

        public async Task<List<WeatherForecast>> GetForecastRangeAsync(
    double latitude,
    double longitude,
    DateTime from,
    DateTime to)
        {
            var apiData = await GetDailyAsync(
                latitude,
                longitude,
                from,
                to);

            return apiData
                .Select(x => new WeatherForecast
                {
                    Id = Guid.NewGuid(),
                    ForecastDate = x.Date.Date,
                    TemperatureCelsius = x.MaxTemperature,
                    PrecipitationProbability = x.PrecipitationProbability,
                    WindSpeed = x.MaxWindSpeed,
                    FetchedAt = DateTime.UtcNow
                })
                .ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IWeatherForecastRepository _weatherRepo;
        private readonly IOpenMeteoService _openMeteoService;
        private readonly ILocationRepository _locationRepo;

        public WeatherService(
            IWeatherForecastRepository weatherRepo,
            IOpenMeteoService openMeteoService,
            ILocationRepository locationRepo)
        {
            _weatherRepo = weatherRepo;
            _openMeteoService = openMeteoService;
            _locationRepo = locationRepo;
        }

        public async Task<WeatherForecast> GetAsync(Guid locationId, DateOnly date)
        {
            var forecast = await _weatherRepo.GetAsync(locationId, date);

            bool needFetch = forecast == null ||
                             forecast.FetchedAt < DateTime.UtcNow.AddHours(-6);

            if (!needFetch)
                return forecast;

            // 🔥 Fetch from API
            var newForecast = await _openMeteoService.GetAsync(locationId, date);

            newForecast.LocationId = locationId;
            newForecast.ForecastDate = date;
            newForecast.FetchedAt = DateTime.UtcNow;

            await _weatherRepo.UpsertAsync(newForecast);

            return newForecast;
        }

        //public async Task<Dictionary<DateOnly, WeatherForecast>>
        //    GetRangeAsync(Guid locationId, List<DateOnly> dates)
        //{
        //    var tasks = dates.Select(async date =>
        //    {
        //        var forecast = await GetAsync(locationId, date);
        //        return (date, forecast);
        //    });

        //    var results = await Task.WhenAll(tasks);

        //    return results.ToDictionary(x => x.date, x => x.forecast);
        //}

        public async Task<Dictionary<DateOnly, WeatherForecast>>
    GetRangeAsync(Guid locationId, List<DateOnly> dates)
        {
            var result = new Dictionary<DateOnly, WeatherForecast>();

            // 1. Load cached sequentially
            foreach (var date in dates)
            {
                var cached = await GetAsync(locationId, date);
                if (cached != null)
                    result[date] = cached;
            }

            // 2. Find missing dates
            var missingDates = dates
                .Where(d => !result.ContainsKey(d))
                .ToList();

            // 3. Call API in parallel (NO DbContext here)
            var apiTasks = missingDates.Select(async date =>
            {
                var loc = await _locationRepo.GetByIdAsync(locationId); // ⚠ still DB → keep outside if possible

                var apiData = await _openMeteoService.GetDailyAsync(
                    loc.Latitude,
                    loc.Longitude,
                    date,
                    date);

                var dto = apiData.First();

                return new WeatherForecast
                {
                    Id = Guid.NewGuid(),
                    LocationId = locationId,
                    ForecastDate = date,
                    TemperatureCelsius = dto.MaxTemperature,
                    PrecipitationProbability = dto.PrecipitationProbability,
                    WindSpeed = dto.MaxWindSpeed,
                    FetchedAt = DateTime.UtcNow
                };
            });

            var apiResults = await Task.WhenAll(apiTasks);

            // 4. Save sequentially
            foreach (var forecast in apiResults)
            {
                await _weatherRepo.UpsertAsync(new List<WeatherForecast> { forecast });
                result[forecast.ForecastDate] = forecast;
            }

            return result;
        }

        public async Task<Dictionary<DateOnly, WeatherForecast>>
    GetRangeOptimizedAsync(Guid locationId, List<DateOnly> dates)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var validDates = dates
                .Where(d => d >= today)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var result = new Dictionary<DateOnly, WeatherForecast>();

            // ================= LOAD CACHE =================
            foreach (var date in validDates)
            {
                var cached = await _weatherRepo.GetAsync(locationId, date);

                if (cached != null &&
                    cached.FetchedAt >= DateTime.Now.AddHours(-6))
                {
                    result[date] = cached;
                }
            }

            // ================= MISSING DATES =================
            var missingDates = validDates
                .Where(d => !result.ContainsKey(d))
                .ToList();

            if (!missingDates.Any())
                return result;

            var from = missingDates.Min();
            var to = missingDates.Max();

            // 🔥 ONE API CALL
            var loc = await _locationRepo.GetByIdAsync(locationId);

            var apiData = await _openMeteoService.GetDailyAsync(
                loc.Latitude,
                loc.Longitude,
                from,
                to);

            var newForecasts = new List<WeatherForecast>();

            foreach (var dto in apiData)
            {
                var entity = new WeatherForecast
                {
                    Id = Guid.NewGuid(),
                    LocationId = locationId,
                    ForecastDate = dto.Date,
                    TemperatureCelsius = dto.MaxTemperature,
                    PrecipitationProbability = dto.PrecipitationProbability,
                    WindSpeed = dto.MaxWindSpeed,
                    FetchedAt = DateTime.Now
                };

                newForecasts.Add(entity);
                result[dto.Date] = entity;
            }

            // 🔥 SAVE ONCE
            await _weatherRepo.UpsertAsync(newForecasts);

            return result;
        }

        public async Task PreloadAsync(Guid locationId, List<DateOnly> dates)
        {
            foreach (var date in dates)
            {
                await GetAsync(locationId, date);
            }
        }
    }
}

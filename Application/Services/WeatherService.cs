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

        public WeatherService(
            IWeatherForecastRepository weatherRepo,
            IOpenMeteoService openMeteoService)
        {
            _weatherRepo = weatherRepo;
            _openMeteoService = openMeteoService;
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

        public async Task<Dictionary<DateOnly, WeatherForecast>>
            GetRangeAsync(Guid locationId, List<DateOnly> dates)
        {
            var tasks = dates.Select(async date =>
            {
                var forecast = await GetAsync(locationId, date);
                return (date, forecast);
            });

            var results = await Task.WhenAll(tasks);

            return results.ToDictionary(x => x.date, x => x.forecast);
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

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
        private readonly ITripSegmentRepository _segmentRepo;
        private readonly IUnitOfWork _unitOfWork;

        public WeatherService(
            IWeatherForecastRepository weatherRepo,
            IOpenMeteoService openMeteoService,
            ILocationRepository locationRepo,
            ITripSegmentRepository segmentRepo,
            IUnitOfWork unitOfWork)
        {
            _weatherRepo = weatherRepo;
            _openMeteoService = openMeteoService;
            _locationRepo = locationRepo;
            _segmentRepo = segmentRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<WeatherForecast> GetAsync(Guid locationId, DateTime date)
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

        //public async Task<Dictionary<DateTime, WeatherForecast>>
        //    GetRangeAsync(Guid locationId, List<DateTime> dates)
        //{
        //    var tasks = dates.Select(async date =>
        //    {
        //        var forecast = await GetAsync(locationId, date);
        //        return (date, forecast);
        //    });

        //    var results = await Task.WhenAll(tasks);

        //    return results.ToDictionary(x => x.date, x => x.forecast);
        //}

        public async Task<Dictionary<DateTime, WeatherForecast>>
    GetRangeAsync(Guid locationId, List<DateTime> dates)
        {
            var result = new Dictionary<DateTime, WeatherForecast>();

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
                    date.Date,
                    date.Date);

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

        //    public async Task<Dictionary<DateTime, WeatherForecast>>
        //GetRangeOptimizedAsync(Guid locationId, List<DateTime> dates)
        //    {
        //        var today = DateTime.Now;

        //        var validDates = dates
        //            .Where(d => d >= today)
        //            .Distinct()
        //            .OrderBy(d => d)
        //            .ToList();

        //        var result = new Dictionary<DateTime, WeatherForecast>();

        //        // ================= LOAD CACHE =================
        //        foreach (var date in validDates)
        //        {
        //            var cached = await _weatherRepo.GetAsync(locationId, date);

        //            if (cached != null &&
        //                cached.FetchedAt >= DateTime.Now.AddHours(-6))
        //            {
        //                result[date] = cached;
        //            }
        //        }

        //        // ================= MISSING DATES =================
        //        var missingDates = validDates
        //            .Where(d => !result.ContainsKey(d))
        //            .ToList();

        //        if (!missingDates.Any())
        //            return result;

        //        var from = missingDates.Min();
        //        var to = missingDates.Max();

        //        // 🔥 ONE API CALL
        //        var loc = await _locationRepo.GetByIdAsync(locationId);

        //        var apiData = await _openMeteoService.GetDailyAsync(
        //            loc.Latitude,
        //            loc.Longitude,
        //            from,
        //            to);

        //        var newForecasts = new List<WeatherForecast>();

        //        foreach (var dto in apiData)
        //        {
        //            var entity = new WeatherForecast
        //            {
        //                Id = Guid.NewGuid(),
        //                LocationId = locationId,
        //                ForecastDate = dto.Date,
        //                TemperatureCelsius = dto.MaxTemperature,
        //                PrecipitationProbability = dto.PrecipitationProbability,
        //                WindSpeed = dto.MaxWindSpeed,
        //                FetchedAt = DateTime.Now
        //            };

        //            newForecasts.Add(entity);
        //            result[dto.Date] = entity;
        //        }

        //        // 🔥 SAVE ONCE
        //        await _weatherRepo.UpsertAsync(newForecasts);

        //        return result;
        //    }

        public async Task<Dictionary<DateTime, WeatherForecast>>
GetRangeOptimizedAsync(Guid locationId, List<DateTime> dates)
        {
            dates = dates
                //.Select(x => x.Date)
                .Select(x => DateTime.SpecifyKind(x.Date, DateTimeKind.Utc))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var existingForecasts = await _weatherRepo
                .GetByLocationAndDates(locationId, dates);

            var result = existingForecasts
                .ToDictionary(x => DateTime.SpecifyKind(
    x.ForecastDate.Date,
    DateTimeKind.Utc));

            var missingDates = dates
                .Where(d => !result.ContainsKey(d))
                .ToList();

            if (!missingDates.Any())
                return result;

            var location = await _locationRepo.GetByIdAsync(locationId);

            if (location == null)
                return result;

            // ONE API CALL
            var apiForecasts = await _openMeteoService
                .GetForecastRangeAsync(
                    location.Latitude,
                    location.Longitude,
                    missingDates.Min(),
                    missingDates.Max());

            foreach (var forecast in apiForecasts)
            {
                forecast.LocationId = locationId;

                forecast.ForecastDate = DateTime.SpecifyKind(
                    forecast.ForecastDate.Date,
                    DateTimeKind.Utc);

                forecast.FetchedAt = DateTime.UtcNow;
            }

            foreach (var item in apiForecasts)
            {
                var key = DateTime.SpecifyKind(
                    item.ForecastDate.Date,
                    DateTimeKind.Utc);

                result[key] = item;
            }

            // ONE SAVE
            await _weatherRepo.UpsertRangeAsync(apiForecasts);

            return result;
        }

        public async Task PreloadAsync(Guid locationId, List<DateTime> dates)
        {
            foreach (var date in dates)
            {
                await GetAsync(locationId, date);
            }
        }

        /// <inheritdoc />
        //public async Task PreloadTripWeatherAsync(Guid tripId)
        //{
        //    var segments = await _segmentRepo.GetByTripIdAsync(tripId);

        //    // Group segments by location so we make ONE batched API call per location.
        //    var byLocation = segments
        //        .Where(s => s.LocationId != Guid.Empty)
        //        .GroupBy(s => s.LocationId);

        //    foreach (var group in byLocation)
        //    {
        //        var locationId = group.Key;

        //        // Collect all dates across segments that share this location.
        //        var dates = group
        //            .SelectMany(s => Enumerable.Range(
        //                0, (s.EndDate.Date - s.StartDate.Date).Days + 1)
        //                .Select(d => s.StartDate.Date.AddDays(d)))
        //            .Distinct()
        //            .OrderBy(d => d)
        //            .ToList();

        //        // GetRangeOptimizedAsync issues a single OpenMeteo request for the
        //        // min-to-max date range and upserts results into the DB.
        //        await GetRangeOptimizedAsync(locationId, dates);
        //    }
        //}

        public async Task PreloadTripWeatherAsync(Guid tripId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var segments = await _segmentRepo.GetByTripIdAsync(tripId);

                var byLocation = segments
                    .Where(s => s.LocationId != Guid.Empty)
                    .GroupBy(s => s.LocationId);

                foreach (var group in byLocation)
                {
                    var locationId = group.Key;

                    var dates = group
                        .SelectMany(s => Enumerable.Range(
                            0,
                            (s.EndDate.Date - s.StartDate.Date).Days + 1)
                            .Select(d => s.StartDate.Date.AddDays(d)))
                        .Distinct()
                        .OrderBy(d => d)
                        .ToList();

                    await GetRangeOptimizedAsync(locationId, dates);
                }

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}

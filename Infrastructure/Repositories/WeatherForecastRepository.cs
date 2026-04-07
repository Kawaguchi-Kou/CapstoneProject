using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly AppDbContext _context;

        public WeatherForecastRepository(AppDbContext context)
        {
            _context = context;
        }

        //public async Task UpsertCityForecastAsync(string city, IEnumerable<WeatherForecast> newForecasts)
        //{
        //    var existing = await _context.WeatherForecasts
        //.Where(x => x.City == city)
        //.ToListAsync();

        //    foreach (var newItem in newForecasts)
        //    {
        //        var match = existing.FirstOrDefault(x =>
        //            x.ForecastDate.Date == newItem.ForecastDate.Date);

        //        if (match != null)
        //        {
        //            // UPDATE
        //            match.TemperatureCelsius = newItem.TemperatureCelsius;
        //            match.WindSpeed = newItem.WindSpeed;
        //            match.PrecipitationProbability = newItem.PrecipitationProbability;
        //        }
        //        else
        //        {
        //            // INSERT
        //            await _context.WeatherForecasts.AddAsync(newItem);
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //}

        public async Task<WeatherForecast?> GetAsync(
        Guid locationId,
        DateTime date)
        {
            return await _context.WeatherForecasts
                .FirstOrDefaultAsync(x =>
                    x.LocationId == locationId &&
                    x.ForecastDate == date);
        }

        public async Task<List<WeatherForecast>> GetRangeAsync(
            Guid locationId,
            DateTime from,
            DateTime to)
        {
            return await _context.WeatherForecasts
                .Where(x =>
                    x.LocationId == locationId &&
                    x.ForecastDate >= from &&
                    x.ForecastDate <= to)
                .ToListAsync();
        }

        public async Task UpsertAsync(List<WeatherForecast> forecasts)
        {
            foreach (var f in forecasts)
            {
                var existing = await GetAsync(f.LocationId, f.ForecastDate);

                if (existing == null)
                    _context.WeatherForecasts.Add(f);
                else
                {
                    existing.TemperatureCelsius = f.TemperatureCelsius;
                    existing.PrecipitationProbability = f.PrecipitationProbability;
                    existing.WindSpeed = f.WindSpeed;
                    existing.FetchedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpsertAsync(WeatherForecast forecast)
        {
            var existing = await _context.WeatherForecasts
                .FirstOrDefaultAsync(x =>
                    x.LocationId == forecast.LocationId &&
                    x.ForecastDate == forecast.ForecastDate);

            if (existing == null)
            {
                await _context.WeatherForecasts.AddAsync(forecast);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(forecast);
            }
        }

        //public async Task UpsertAsync(WeatherForecast forecast)
        //{
        //    // 1. Normalize DateTimes for PostgreSQL (CRITICAL)
        //    // Ensure ForecastDate is UTC and stripped of time components for consistent lookup
        //    forecast.ForecastDate = DateTime.SpecifyKind(forecast.ForecastDate, DateTimeKind.Utc);
        //    // Ensure FetchedAt is UTC
        //    forecast.FetchedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        //    // 2. Check for existing record based on your LocationId + ForecastDate logic
        //    var existing = await _context.WeatherForecasts
        //        .FirstOrDefaultAsync(x =>
        //            x.LocationId == forecast.LocationId &&
        //            x.ForecastDate == forecast.ForecastDate);

        //    if (existing == null)
        //    {
        //        // 3. New record
        //        await _context.WeatherForecasts.AddAsync(forecast);
        //    }
        //    else
        //    {
        //        // 4. Update existing using your preferred SetValues method
        //        // Note: SetValues updates all mapped properties from the input object
        //        _context.Entry(existing).CurrentValues.SetValues(forecast);

        //        // Ensure the ID of the existing tracked entity isn't overwritten if forecast.Id is empty
        //        _context.Entry(existing).Property(x => x.Id).IsModified = false;
        //    }

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException ex)
        //    {
        //        // If you get a 23505 error here, ensure your DB index matches (LocationId, ForecastDate)
        //        throw new Exception($"Failed to upsert weather for Location {forecast.LocationId} on {forecast.ForecastDate:yyyy-MM-dd}", ex);
        //    }
        //}

        public async Task<List<WeatherForecast>> GetByLocationAndDates(
        Guid locationId,
        List<DateTime> dates)
        {
            if (dates == null || !dates.Any())
                return new List<WeatherForecast>();

            dates = dates.Distinct().ToList();

            return await _context.WeatherForecasts
                .AsNoTracking()
                .Where(x => x.LocationId == locationId && dates.Contains(x.ForecastDate))
                .ToListAsync();
        }

        public async Task<Dictionary<DateTime, WeatherForecast>> GetByLocationAndDatesDict(
    Guid locationId,
    List<DateTime> dates)
        {
            if (dates == null || !dates.Any())
                return new Dictionary<DateTime, WeatherForecast>();

            dates = dates.Distinct().ToList();

            return await _context.WeatherForecasts
                .AsNoTracking()
                .Where(x => x.LocationId == locationId && dates.Contains(x.ForecastDate))
                .ToDictionaryAsync(x => x.ForecastDate);
        }
    }
}

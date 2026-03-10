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

        public async Task UpsertCityForecastAsync(string city, IEnumerable<WeatherForecast> newForecasts)
        {
            var existing = await _context.WeatherForecasts
        .Where(x => x.City == city)
        .ToListAsync();

            foreach (var newItem in newForecasts)
            {
                var match = existing.FirstOrDefault(x =>
                    x.ForecastDate.Date == newItem.ForecastDate.Date);

                if (match != null)
                {
                    // UPDATE
                    match.TemperatureCelsius = newItem.TemperatureCelsius;
                    match.WindSpeed = newItem.WindSpeed;
                    match.PrecipitationProbability = newItem.PrecipitationProbability;
                }
                else
                {
                    // INSERT
                    await _context.WeatherForecasts.AddAsync(newItem);
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}

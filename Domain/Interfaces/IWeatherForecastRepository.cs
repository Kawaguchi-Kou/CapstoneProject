using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IWeatherForecastRepository
    {
        //Task UpsertCityForecastAsync(string city, IEnumerable<WeatherForecast> newForecasts);

        Task<WeatherForecast?> GetAsync(Guid locationId, DateOnly date);

        Task<List<WeatherForecast>> GetRangeAsync(
            Guid locationId,
            DateOnly from,
            DateOnly to);

        Task UpsertAsync(List<WeatherForecast> forecasts);
    }
}

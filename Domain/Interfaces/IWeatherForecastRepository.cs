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

        Task<WeatherForecast?> GetAsync(Guid locationId, DateTime date);

        Task<List<WeatherForecast>> GetRangeAsync(
            Guid locationId,
            DateTime from,
            DateTime to);

        Task UpsertAsync(List<WeatherForecast> forecasts);

        Task UpsertAsync(WeatherForecast forecast);

        Task<List<WeatherForecast>> GetByLocationAndDates(
        Guid locationId,
        List<DateTime> dates);

        Task<Dictionary<DateTime, WeatherForecast>> GetByLocationAndDatesDict(
    Guid locationId,
    List<DateTime> dates);

        Task SaveChangesAsync();
    }
}

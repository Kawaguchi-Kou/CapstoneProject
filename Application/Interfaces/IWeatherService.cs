using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherForecast> GetAsync(Guid locationId, DateOnly date);

        Task<Dictionary<DateOnly, WeatherForecast>>
            GetRangeAsync(Guid locationId, List<DateOnly> dates);

        Task<Dictionary<DateOnly, WeatherForecast>>
            GetRangeOptimizedAsync(Guid locationId, List<DateOnly> dates);

        Task PreloadAsync(Guid locationId, List<DateOnly> dates);
    }
}

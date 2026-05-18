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
        Task<WeatherForecast> GetAsync(Guid locationId, DateTime date);

        Task<Dictionary<DateTime, WeatherForecast>>
            GetRangeAsync(Guid locationId, List<DateTime> dates);

        Task<Dictionary<DateTime, WeatherForecast>>
            GetRangeOptimizedAsync(Guid locationId, List<DateTime> dates);

        Task PreloadAsync(Guid locationId, List<DateTime> dates);

        /// <summary>
        /// Fetches and caches fresh weather data from OpenMeteo for every segment of the given trip.
        /// </summary>
        Task PreloadTripWeatherAsync(Guid tripId);
    }
}

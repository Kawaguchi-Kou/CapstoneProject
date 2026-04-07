using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Weather;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IOpenMeteoService
    {
        /// <summary>
        /// Lấy dữ liệu thời tiết theo NGÀY cho phượt
        /// (đã map từ Open-Meteo)
        /// </summary>
        Task<IReadOnlyList<DailyWeatherDto>> GetDailyAsync(
            double latitude,
            double longitude,
            DateTime from,
            DateTime to);

        /// <summary>
        /// Lấy dữ liệu thời tiết cho 1 NGÀY
        /// (đã map từ Open-Meteo)
        /// </summary>
        Task<DailyWeatherDto?> GetSingleDayAsync(
            double lat,
            double lng,
            DateTime date);

        Task<WeatherForecast> GetAsync(
            Guid locationId,
            DateTime date);
    }
}

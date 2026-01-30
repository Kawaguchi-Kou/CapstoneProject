using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Weather;

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
            DateOnly from,
            DateOnly to);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Weather
{
    public class DailyWeatherDto
    {
        public DateOnly Date { get; init; }

        /// <summary>
        /// °C
        /// </summary>
        public double MaxTemperature { get; init; }

        /// <summary>
        /// % (0–100)
        /// </summary>
        public double PrecipitationProbability { get; init; }

        /// <summary>
        /// km/h
        /// </summary>
        public double MaxWindSpeed { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Weather
{
    public class WeatherData
    {
        public double RainProbability { get; set; }
        public double TemperatureCelsius { get; set; }

        public string Condition { get; set; } = string.Empty;

        public string TimeSlot { get; set; } = string.Empty; // morning / afternoon
    }
}

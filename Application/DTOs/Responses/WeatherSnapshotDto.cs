using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class WeatherSnapshotDto
    {
        public double TemperatureCelsius { get; set; }
        public double PrecipitationProbability { get; set; }
        public double WindSpeed { get; set; }
    }
}

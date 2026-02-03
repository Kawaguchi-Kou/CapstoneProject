using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WeatherForecast
    {
        [Key]
        public Guid Id { get; set; }
        public string City { get; set; } = string.Empty;
        public DateTime ForecastDate { get; set; }
        public double TemperatureCelsius { get; set; }
        public double WindSpeed { get; set; }
        public double PrecipitationProbability { get; set; }
    }
}

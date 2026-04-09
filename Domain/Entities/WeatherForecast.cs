using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class WeatherForecast
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid LocationId { get; set; }  // Liên kết với Location để biết thời tiết của địa điểm nào
        public string City { get; set; } = string.Empty;
        public DateTime ForecastDate { get; set; }
        public double TemperatureCelsius { get; set; }
        public double WindSpeed { get; set; }
        public double PrecipitationProbability { get; set; }
        public DateTime FetchedAt { get; set; }

        //Navigation
        [ForeignKey(nameof(LocationId))]
        public Location Location { get; set; }

    }
}

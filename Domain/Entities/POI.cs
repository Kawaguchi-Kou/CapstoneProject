using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class POI
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ApproxCost { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        public Guid ForecastId { get; set; }
        [ForeignKey(nameof(ForecastId))]
        public WeatherForecast Forecast { get; set; }
        public ICollection<POIPreference> PoiPreferences { get; set; }
    }
}

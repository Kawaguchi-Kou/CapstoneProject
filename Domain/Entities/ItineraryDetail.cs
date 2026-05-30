using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ItineraryDetail
    {
        [Key]
        public Guid DetailId { get; set; }
        [Required]
        public Guid ItineraryId { get; set; }
        public DateTime VisitDate { get; set; }
        public Guid? PoiId { get; set; }
        public double TemperatureCelsius { get; set; }

        public double PrecipitationProbability { get; set; }

        public double WindSpeed { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime RiskCalculatedAt { get; set; }
        public bool IsManualOverride { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation
        [ForeignKey(nameof(ItineraryId))]
        public Itinerary Itinerary { get; set; }

        [ForeignKey(nameof(PoiId))]
        public POI? POI { get; set; }

    }
}

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

        public DateTime Date { get; set; }

        public Guid? PoiId { get; set; }

        public double WeatherRiskScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: nếu muốn gắn chi tiết theo chặng
        public Guid? SegmentId { get; set; }

        // Navigation
        [ForeignKey(nameof(ItineraryId))]
        public Itinerary Itinerary { get; set; }

        [ForeignKey(nameof(SegmentId))]
        public TripSegment TripSegment { get; set; }

        public ICollection<ManualOverride> Overrides { get; set; }
    }
}

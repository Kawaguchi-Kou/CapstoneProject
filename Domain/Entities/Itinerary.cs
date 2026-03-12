using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Itinerary
    {
        [Key]
        public Guid ItineraryId { get; set; }

        [Required]
        public Guid SegmentId { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool GeneratedByAI { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } 

        // Navigation
        [ForeignKey(nameof(SegmentId))]
        public TripSegment Segment { get; set; }

        public ICollection<ItineraryDetail>? ItineraryDetails { get; set; }
    }
}

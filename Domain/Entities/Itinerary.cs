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
        public Guid TripId { get; set; }

        public int Version { get; set; }

        public bool IsActive { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(TripId))]
        public Trip Trip { get; set; }

        public ICollection<ItineraryDetail> ItineraryDetails { get; set; }
    }
}

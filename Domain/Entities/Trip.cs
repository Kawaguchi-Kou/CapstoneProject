using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Trip
    {
        [Key]
        public Guid TripId { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string StartLocation { get; set; }

        [Required]
        [MaxLength(255)]
        public string EndLocation { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? PreferencesId { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<TripSegment> TripSegments { get; set; }
        public ICollection<Itinerary> Itineraries { get; set; }
    }
}

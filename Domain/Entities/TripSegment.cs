using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class TripSegment
    {
        [Key]
        public Guid SegmentId { get; set; }

        [Required]
        public Guid TripId { get; set; }

        [Required]
        public Guid LocationId { get; set; }
        public Guid? DistrictId { get; set; }

        [Required]
        public int OrderIndex { get; set; }   // Thứ tự chặng

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public double? DistanceKm { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(TripId))]
        public Trip Trip { get; set; }

        [ForeignKey(nameof(DistrictId))]
        public District District { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location Location { get; set; }
        public ICollection<Itinerary>? Itineraries { get; set; }
    }
}

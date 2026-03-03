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
        public int SequenceNo { get; set; }   // Thứ tự chặng

        [Required]
        [MaxLength(255)]
        public string FromLocation { get; set; }

        [Required]
        [MaxLength(255)]
        public string ToLocation { get; set; }

        public DateTime? TravelDate { get; set; }

        public float? DistanceKm { get; set; }

        public int? EstimatedMinutes { get; set; }

        public SegmentType SegmentType { get; set; } = SegmentType.Waypoint;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(TripId))]
        public Trip Trip { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class Trip
    {
        [Key]
        public Guid TripId { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        
        [MaxLength(255)]
        public string Title { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TripStatus Status { get; set; }

        public TripType TripType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<TripSegment> TripSegments { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Participant> Participants { get; set; }

    }
}

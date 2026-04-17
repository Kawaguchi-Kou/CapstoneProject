using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class TripSegmentResponse
    {
        public Guid SegmentId { get; set; }
        public Guid TripId { get; set; }
        public Guid LocationId { get; set; }
        public int OrderIndex { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public float? DistanceKm { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

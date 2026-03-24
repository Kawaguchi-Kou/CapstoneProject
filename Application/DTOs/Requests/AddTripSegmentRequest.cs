using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class AddTripSegmentRequest
    {
        public Guid LocationId { get; set; }
        public int OrderIndex { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public float? DistanceKm { get; set; }
    }
}

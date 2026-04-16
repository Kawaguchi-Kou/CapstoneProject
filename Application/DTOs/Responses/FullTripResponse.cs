using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.AIResponse;

namespace Application.DTOs.Responses
{
    public class FullTripResponse
    {
        public Guid TripId { get; set; }
        public List<SegmentResponse> Segments { get; set; } = new();
    }
}

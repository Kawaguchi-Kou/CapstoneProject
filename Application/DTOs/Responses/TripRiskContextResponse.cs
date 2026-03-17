using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class TripRiskContextResponse
    {
        public Guid TripId { get; set; }
        public Guid AccountId { get; set; }

        public List<SegmentRiskContextResponse>? Segments { get; set; }
    }
}

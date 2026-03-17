using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class SegmentRiskContextResponse
    {
        public Guid SegmentId { get; set; }

        public List<ItineraryRiskContextResponse> Details { get; set; }
    }
}

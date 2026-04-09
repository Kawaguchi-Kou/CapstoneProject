using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class ItineraryRiskContextResponse
    {
        public Guid DetailId { get; set; }

        public Guid LocationId { get; set; }

        public Guid PoiId { get; set; }

        public DateTime PlannedDate { get; set; }

        public double StoredRiskScore { get; set; }
    }
}

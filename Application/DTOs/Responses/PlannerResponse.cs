using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class PlannerResponse
    {
        public Guid TripId { get; set; }

        public bool IsSafe { get; set; }

        public string GlobalRecommendation { get; set; }

        public List<SegmentPlanResponse> Segments { get; set; } = new();

    }
}

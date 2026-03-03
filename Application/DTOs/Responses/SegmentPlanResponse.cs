using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class SegmentPlanResponse
    {
        public Guid SegmentId { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public double RiskScore { get; set; }

        public double Threshold { get; set; }

        public string Zone { get; set; }

        public string Recommendation { get; set; }

        public bool IsSafe { get; set; }
    }
}

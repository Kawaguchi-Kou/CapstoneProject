using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class SegmentResponse
    {
        public Guid SegmentId { get; set; }
        public int OrderIndex { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<DayPlanResponse> Days { get; set; } = new();
    }
}

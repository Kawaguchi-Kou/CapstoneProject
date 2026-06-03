using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class AIActivity
    {
        public Guid PoiId { get; set; }

        public Guid SegmentId { get; set; }

        public string Period { get; set; }

        public int DurationMinutes { get; set; }

        public string Reason { get; set; }
    }
}

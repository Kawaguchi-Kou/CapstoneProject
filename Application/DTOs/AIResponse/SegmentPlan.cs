using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class SegmentPlan
    {
        public int OrderIndex { get; set; }
        public List<AIDayPlan> Days { get; set; } = new();
    }
}

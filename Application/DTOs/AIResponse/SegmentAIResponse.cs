using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class SegmentAIResponse
    {
        public List<AIDayPlan> Days { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class AISegmentPlan
    {
        [JsonPropertyName("segmentOrder")]
        public int SegmentOrder { get; set; }

        [JsonPropertyName("days")]
        public List<AIDayPlan> Days { get; set; } = [];
    }
}

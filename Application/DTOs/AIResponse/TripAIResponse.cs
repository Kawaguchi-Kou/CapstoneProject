using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class TripAIResponse
    {
        [JsonPropertyName("segments")]
        public List<AISegmentPlan> Segments { get; set; } = [];
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class AIActivity
    {
        [JsonPropertyName("poiId")]
        public Guid PoiId { get; set; }

        [JsonPropertyName("period")]
        public string Period { get; set; } = string.Empty;

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}

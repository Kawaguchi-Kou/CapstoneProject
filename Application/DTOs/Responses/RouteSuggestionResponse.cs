using System.Collections.Generic;

namespace Application.DTOs.Responses
{
    public class RouteSuggestionResponse
    {
        public string RouteId { get; set; } = string.Empty;

        public int RouteIndex { get; set; }

        public List<RouteStopDto> Stops { get; set; } = new();

        public double TotalDistanceKm { get; set; }

        public string WeatherSummary { get; set; } = string.Empty;

        public string AiRecommendation { get; set; } = string.Empty;

        public bool Recommended { get; set; }

        public double Score { get; set; }

        public List<SegmentReasonDetail> Reasons { get; set; } = new();
    }
}

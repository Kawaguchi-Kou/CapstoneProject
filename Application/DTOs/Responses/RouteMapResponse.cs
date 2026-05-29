using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class RouteMapResponse
    {
        public string RouteId { get; set; } = "";

        public int RouteIndex { get; set; }

        public double TotalDistanceKm { get; set; }

        // dùng để vẽ line trên map
        public List<RoutePolylineDto> Polylines { get; set; } = new();

        // marker/stops
        public List<RouteStopDto> Stops { get; set; } = new();

        // weather summary
        public string WeatherSummary { get; set; } = "";

        // AI advice
        public string TravelAdvice { get; set; } = "";

        // warnings
        public List<string> Warnings { get; set; } = new();
    }
}

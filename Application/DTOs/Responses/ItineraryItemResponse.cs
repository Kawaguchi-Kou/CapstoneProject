using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class ItineraryItemResponse
    {
        public string Type { get; set; } = string.Empty; // Breakfast, Activity, etc
        public string PoiName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool IsIndoor { get; set; }
        public string POIImg { get; set; } = string.Empty;
        public string? AIReason { get; set; }
        public WeatherSnapshotDto? Weather { get; set; }
    }
}

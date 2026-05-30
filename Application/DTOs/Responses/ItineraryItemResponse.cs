using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class ItineraryItemResponse
    {
        public string Type { get; set; } = default!; // Breakfast, Activity, etc
        public string PoiName { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string LocationName { get; set; } = default!;
        public bool IsIndoor { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Period { get; set; } = default!;

        public WeatherSnapshotDto? Weather { get; set; }
    }
}

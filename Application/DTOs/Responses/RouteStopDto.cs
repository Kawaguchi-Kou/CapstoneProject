using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class RouteStopDto
    {
        public string NodeId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        /// <summary>Km from the previous stop (0 for the first stop).</summary>
        public double DistanceFromPrevKm { get; set; }

        public string RouteType { get; set; } = string.Empty;

        /// <summary>Null when the node has no matching DB location.</summary>
        public WeatherSnapshotDto? Weather { get; set; }
    }
}

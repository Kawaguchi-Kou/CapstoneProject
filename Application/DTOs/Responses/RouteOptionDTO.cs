using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.DTOs.Responses
{
    public class RouteOptionDTO
    {
        public string RouteId { get; set; } = string.Empty;

        public int RouteIndex { get; set; }

        public double TotalDistanceKm { get; set; }

        // internal graph ids
        public List<string> NodeIds { get; set; } = new();

        // display names
        public List<string> Nodes { get; set; } = new();

        public List<GraphEdge> Edges { get; set; } = new();

        public List<RoutePolylinePointDto> Polyline { get; set; } = new();
    }
}

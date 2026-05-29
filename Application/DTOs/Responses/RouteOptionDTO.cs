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
        public string RouteId { get; set; } = "";

        public int RouteIndex { get; set; }

        public double TotalDistanceKm { get; set; }

        public List<string> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();

        public List<RoutePolylinePointDto> Polyline { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class RoutePolylineDto
    {
        public string RouteType { get; set; } = "";

        // Google Maps polyline encoded string
        public string EncodedPolyline { get; set; } = "";

        //// fallback if FE muốn custom draw
        //public List<LatLngDto> Coordinates { get; set; } = new();
    }
}

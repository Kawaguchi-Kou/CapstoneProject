using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class POIResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ApproxCost { get; set; }
        public string OpeningHours { get; set; }
        public string GoogleMapLink { get; set; }
        public bool IsIndoor { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid LocationId { get; set; }
    }
}

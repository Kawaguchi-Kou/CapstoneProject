using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class PoiResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string ApproxCost { get; set; } = string.Empty;

        public TimeOnly OpenHour { get; set; } 
        public TimeOnly CloseHour { get; set; }

        public string GoogleMapLink { get; set; } = string.Empty;
        public string POIImgUrl { get; set; } = string.Empty;

        public bool IsIndoor { get; set; }

        public POIType Type { get; set; }

        public POIStatus Status { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public Guid LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public List<string> Preferences { get; set; } = new List<string>();
    }
}

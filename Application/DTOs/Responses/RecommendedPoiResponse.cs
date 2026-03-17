using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.DTOs.Responses
{
    public class RecommendedPoiResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ApproxCost { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = string.Empty;
        public string GoogleMapLink { get; set; } = string.Empty;
        public string POIImgUrl { get; set; } = string.Empty;
        public bool IsIndoor { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string> POIPreferences { get; set; } = new List<string>();
    }

}

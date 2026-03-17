using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class CreatePoiRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string ApproxCost { get; set; } = string.Empty;

        public string OpeningHours { get; set; } = string.Empty;

        public string GoogleMapLink { get; set; } = string.Empty;
        public IFormFile? POIImgUrl { get; set; }

        public bool IsIndoor { get; set; }
    }
}

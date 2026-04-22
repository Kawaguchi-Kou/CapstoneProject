using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class UpdatePoiRequest
    {
        public string? Address { get; set; } = string.Empty;
        public string? ApproxCost { get; set; } = string.Empty;
        public string? OpeningHours { get; set; } = string.Empty;
        public string? GoogleMapLink { get; set; } = string.Empty;
        public bool? IsIndoor { get; set; }
        public IFormFile? POIImgUrl { get; set; }
        public string? Name { get; set; } = string.Empty;

        public TimeOnly? OpenHour { get; set; }
        public TimeOnly? CloseHour { get; set; }
        public bool? Is24Hours { get; set; }
        public string? VisitRecommendation { get; set; }
        public POIType? Type { get; set; }
    }
}

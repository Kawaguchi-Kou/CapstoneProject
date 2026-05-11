using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class CreatePoiRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        // City is represented by LocationId

        public string ApproxCost { get; set; } = string.Empty;

        public TimeOnly OpenHour { get; set; }
        public TimeOnly CloseHour { get; set; }
        [Required]
        public Guid LocationId { get; set; }
        [Required]
        public Guid DistrictId { get; set; }

        public string GoogleMapLink { get; set; } = string.Empty;
        public IFormFile? POIImgUrl { get; set; }
        public string? VisitRecommendation { get; set; }

        public bool IsIndoor { get; set; }
        
        public Domain.Enums.POIType Type { get; set; }

        public List<Guid> PoiPreferences { get; set; }
    }
}

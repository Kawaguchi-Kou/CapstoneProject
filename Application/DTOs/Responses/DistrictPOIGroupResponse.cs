using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class DistrictPOIGroupResponse
    {
        public Guid DistrictId { get; set; }

        public string DistrictName { get; set; } = string.Empty;

        public List<POIItemResponse> POIs { get; set; } = new();
    }
}

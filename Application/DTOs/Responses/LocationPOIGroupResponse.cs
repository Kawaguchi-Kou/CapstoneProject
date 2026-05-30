using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class LocationPOIGroupResponse
    {
        public Guid LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public List<DistrictPOIGroupResponse> Districts { get; set; } = new();
    }
}

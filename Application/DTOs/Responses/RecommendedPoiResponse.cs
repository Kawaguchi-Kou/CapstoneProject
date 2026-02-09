using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class RecommendedPoiResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApproxCost { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Score { get; set; }
    }

}

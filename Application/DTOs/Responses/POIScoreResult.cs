using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class POIScoreResult
    {
        public Guid PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}

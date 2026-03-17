using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class TripRiskScanResponse
    {
        public Guid TripId { get; set; }
        public Guid AccountId { get; set; }

        public bool HasHighRisk { get; set; }

        public List<Guid> AffectedDetails { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.DTOs.Responses
{
    public class POIScoreResult
    {
        public Guid PoiId { get; set; }
        public string PoiName { get; set; }
        public int Score { get; set; } // COUNT-based
    }
}

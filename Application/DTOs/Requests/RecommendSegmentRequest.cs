using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class RecommendSegmentRequest
    {
        public Guid StartLocationId { get; set; }

        public Guid? EndLocationId { get; set; } // optional

        public DateTime StartDate { get; set; }

        public int MaxStops { get; set; } = 5;
    }
}

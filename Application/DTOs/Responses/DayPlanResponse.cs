using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class DayPlanResponse
    {
        public DateTime Date { get; set; }
        public string? DayReason { get; set; }
        public List<ItineraryItemResponse> Items { get; set; } = new();
    }
}

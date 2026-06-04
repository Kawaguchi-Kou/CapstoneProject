using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class TripAIResponse
    {
        public List<AIDayPlan> Days { get; set; } = new();
    }
}

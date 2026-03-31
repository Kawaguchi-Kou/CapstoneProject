using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class AIDayPlan
    {
        public DateOnly Date { get; set; }
        public List<AIItem> Plan { get; set; }
    }
}

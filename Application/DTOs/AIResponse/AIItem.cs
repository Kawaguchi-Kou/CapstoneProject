using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AIResponse
{
    public class AIItem
    {
        public string Type { get; set; } // Breakfast, Lunch...
        public string Poi { get; set; }
        public string Time { get; set; }
    }
}

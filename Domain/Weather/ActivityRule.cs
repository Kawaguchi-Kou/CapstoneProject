using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Weather
{
    public class ActivityRule
    {
        public static double RequiredScore(ActivityType type) => type switch
        {
            ActivityType.OutdoorHeavy => 0.7,
            ActivityType.OutdoorLight => 0.55,
            ActivityType.Indoor => 0.35,
            _ => 0.5
        };
    }
}

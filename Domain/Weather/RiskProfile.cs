using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Weather
{
    public class RiskProfile
    {
        public double PrecipitationWeight { get; init; }
        public double WindWeight { get; init; }
        public double TemperatureWeight { get; init; }
    }
}

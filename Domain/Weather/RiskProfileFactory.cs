using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Weather
{
    public static class RiskProfileFactory
    {
        public static class PhuotRiskProfileFactory
        {
            public static RiskProfile Resolve(RouteType routeType)
            {
                return routeType switch
                {
                    RouteType.MountainPass => new RiskProfile
                    {
                        PrecipitationWeight = 0.45,
                        WindWeight = 0.35,
                        TemperatureWeight = 0.2
                    },

                    RouteType.Coastal => new RiskProfile
                    {
                        PrecipitationWeight = 0.4,
                        WindWeight = 0.45,
                        TemperatureWeight = 0.15
                    },

                    RouteType.Delta => new RiskProfile
                    {
                        PrecipitationWeight = 0.55,
                        WindWeight = 0.25,
                        TemperatureWeight = 0.2
                    },

                    RouteType.InterCity => new RiskProfile
                    {
                        PrecipitationWeight = 0.5,
                        WindWeight = 0.3,
                        TemperatureWeight = 0.2
                    },

                    _ => new RiskProfile
                    {
                        PrecipitationWeight = 0.5,
                        WindWeight = 0.3,
                        TemperatureWeight = 0.2
                    }
                };
            }
        }
    }
}

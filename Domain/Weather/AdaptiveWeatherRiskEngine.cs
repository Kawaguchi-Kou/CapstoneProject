using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
//using static Domain.Weather.RiskProfileFactory;

namespace Domain.Weather
{
    public class AdaptiveWeatherRiskEngine
    {
        //public double CalculateRisk(
        //        double temperature,
        //        double windSpeed,
        //        double precipitationProbability,
        //        RouteType routeType)
        //{
        //    var profile = PhuotRiskProfileFactory.Resolve(routeType);

        //    var T = NormalizeTemperature(temperature);
        //    var W = NormalizeWind(windSpeed);
        //    var P = NormalizePrecipitation(precipitationProbability);

        //    var baseRisk =
        //        (profile.PrecipitationWeight * P)
        //      + (profile.WindWeight * W)
        //      + (profile.TemperatureWeight * T);

        //    var routeMultiplier = GetRouteRiskMultiplier(routeType);

        //    return Math.Min(baseRisk * routeMultiplier, 1.0);
        //}

        public double CalculateRisk(
        double temperature,
        double windSpeed,
        double precipitation)
        {
            var T = NormalizeTemperature(temperature);
            var W = NormalizeWind(windSpeed);
            var P = NormalizePrecipitation(precipitation);

            return 0.5 * P + 0.3 * W + 0.2 * T;
        }

        private static double NormalizeTemperature(double temperature)
        {
            return Math.Min(Math.Abs(temperature - 22) / 15.0, 1.0);
        }

        private static double NormalizeWind(double windSpeed)
        {
            return Math.Min(windSpeed / 20.0, 1.0);
        }

        private static double NormalizePrecipitation(double precipitationProbability)
        {
            return Math.Clamp(precipitationProbability / 100.0, 0.0, 1.0);
        }


        //private static double GetRouteRiskMultiplier(RouteType routeType)
        //{
        //    return routeType switch
        //    {
        //        RouteType.MountainPass => 1.25,   // đèo nguy hiểm hơn
        //        RouteType.Coastal => 1.15,        // gió biển
        //        RouteType.Delta => 1.05,
        //        RouteType.InterCity => 1.0,
        //        _ => 1.0
        //    };
        //}
    }
}

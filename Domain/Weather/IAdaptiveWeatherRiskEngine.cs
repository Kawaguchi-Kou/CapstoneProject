using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Weather
{
    public interface IAdaptiveWeatherRiskEngine
    {
        double CalculateRisk(WeatherForecast forecast, bool isIndoor);
    }
}

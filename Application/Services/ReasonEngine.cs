using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;
using Domain.Enums;
using Domain.Entities;
using Application.DTOs.Weather;

namespace Application.Services
{
    public class ReasonEngine
    {
        public List<SegmentReasonDetail> Evaluate(TripSegment segment, WeatherData weather)
        {
            var reasons = new List<SegmentReasonDetail>();

            // Weather logic
            if (weather.RainProbability < 0.3)
            {
                reasons.Add(new SegmentReasonDetail
                {
                    Reason = SegmentReason.GoodWeather,
                    Metadata = new()
                    {
                        ["rainProbability"] = weather.RainProbability,
                        ["temperature"] = weather.TemperatureCelsius
                    }
                });
            }
            else if (weather.RainProbability > 0.7)
            {
                reasons.Add(new SegmentReasonDetail
                {
                    Reason = SegmentReason.BadWeather,
                    Metadata = new()
                    {
                        ["rainProbability"] = weather.RainProbability
                    }
                });
            }

            // Distance
            if (segment.DistanceKm < 200)
                reasons.Add(new() { Reason = SegmentReason.ShortDistance });

            if (segment.DistanceKm > 350)
                reasons.Add(new() { Reason = SegmentReason.LongDistance });

            return reasons;
        }
    }
}

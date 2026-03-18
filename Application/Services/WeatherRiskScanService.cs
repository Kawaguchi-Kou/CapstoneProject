using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Weather;

namespace Application.Services
{
    public class WeatherRiskScanService : IWeatherRiskScanService
    {
        private readonly ITripQueryService _tripQuery;
        private readonly IOpenMeteoService _weather;
        private readonly IAdaptiveWeatherRiskEngine _engine;
        private readonly IPOIRepository _poiRepo;

        public WeatherRiskScanService(
            ITripQueryService tripQuery,
            IOpenMeteoService weather,
            IAdaptiveWeatherRiskEngine engine,
            IPOIRepository poiRepo)
        {
            _tripQuery = tripQuery;
            _weather = weather;
            _engine = engine;
            _poiRepo = poiRepo;
        }

        public async Task<TripRiskScanResponse> ScanAsync(Guid tripId)
        {
            var ctx = await _tripQuery.GetRiskContextAsync(tripId);

            var result = new TripRiskScanResponse
            {
                TripId = tripId,
                AccountId = ctx.AccountId,
                HasHighRisk = false,
                AffectedDetails = new List<Guid>()
            };

            foreach (var seg in ctx.Segments!)
            {
                foreach (var d in seg.Details)
                {
                    var weather = await _weather.GetAsync(
                        d.LocationId,
                        d.PlannedDate);

                    var poi = await _poiRepo.GetByIdAsync(d.PoiId);

                    var risk = _engine.CalculateRisk(weather, poi!.IsIndoor);

                    if (risk >= 0.8)
                    {
                        result.HasHighRisk = true;
                        result.AffectedDetails.Add(d.DetailId);
                    }
                }
            }

            return result;
        }
    }
}

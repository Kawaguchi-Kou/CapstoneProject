using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Weather;

namespace Application.Services
{
    public class PlannerService : IPlannerService
    {
        private readonly IPOIRepository _poiRepo;
        private readonly IWeatherForecastRepository _weatherRepo;
        private readonly IItineraryRepository _itineraryRepo;
        private readonly IItineraryDetailRepository _detailRepo;
        private readonly ITripSegmentRepository _segmentRepo;
        private readonly IAdaptiveWeatherRiskEngine _riskEngine;
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);
        private static readonly TimeSpan Buffer = TimeSpan.FromMinutes(30);

        public PlannerService(
            IPOIRepository poiRepo,
            IWeatherForecastRepository weatherRepo,
            IItineraryRepository itineraryRepo,
            IItineraryDetailRepository detailRepo,
            IAdaptiveWeatherRiskEngine riskEngine,
            ITripSegmentRepository segmentRepo)
        {
            _poiRepo = poiRepo;
            _weatherRepo = weatherRepo;
            _itineraryRepo = itineraryRepo;
            _detailRepo = detailRepo;
            _riskEngine = riskEngine;
            _segmentRepo = segmentRepo;
        }

        //public async Task GenerateAsync(Guid tripId)
        //{
        //    var segments = await _segmentRepo.GetByTripIdAsync(tripId);

        //    foreach (var segment in segments)
        //    {
        //        var itinerary = new Itinerary
        //        {
        //            ItineraryId = Guid.NewGuid(),
        //            SegmentId = segment.SegmentId,
        //            GeneratedByAI = true
        //        };

        //        await _itineraryRepo.AddAsync(itinerary);

        //        var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);

        //        var currentDate = segment.StartDate;

        //        while (currentDate <= segment.EndDate)
        //        {
        //            var forecast = await _weatherRepo
        //                .GetAsync(segment.LocationId, currentDate);

        //            var usedPoiIds = new HashSet<Guid>();
        //            var details = new List<ItineraryDetail>();

        //            var currentTime = new TimeOnly(8, 0); // start day
        //            var endOfDay = new TimeOnly(21, 0);

        //            while (currentTime < endOfDay)
        //            {
        //                var poi = SelectBestPOI(pois, forecast, usedPoiIds);

        //                if (poi == null)
        //                {
        //                    currentTime = currentTime.AddMinutes(30);
        //                    continue;
        //                }

        //                var duration = EstimateDuration(poi);
        //                var endTime = currentTime.Add(duration);

        //                if (!IsValidTime(poi, currentTime, endTime))
        //                {
        //                    currentTime = currentTime.AddMinutes(30);
        //                    continue;
        //                }

        //                var risk = _riskEngine.CalculateRisk(forecast, poi.IsIndoor);

        //                details.Add(new ItineraryDetail
        //                {
        //                    DetailId = Guid.NewGuid(),
        //                    ItineraryId = itinerary.ItineraryId,
        //                    PoiId = poi.Id,
        //                    VisitDate = currentDate,
        //                    StartTime = currentTime,
        //                    EndTime = endTime,
        //                    WeatherRiskScore = risk
        //                });

        //                usedPoiIds.Add(poi.Id);

        //                currentTime = endTime.AddMinutes(30); // buffer
        //            }

        //            await _detailRepo.AddRangeAsync(details);

        //            currentDate = currentDate.AddDays(1);
        //        }
        //    }
        //}

        public async Task GenerateAsync(Guid tripId)
        {
            var segments = await _segmentRepo.GetByTripIdAsync(tripId);

            foreach (var segment in segments)
            {
                var itinerary = new Itinerary
                {
                    ItineraryId = Guid.NewGuid(),
                    SegmentId = segment.SegmentId,
                    GeneratedByAI = true
                };

                await _itineraryRepo.AddAsync(itinerary);

                var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);

                var currentDate = segment.StartDate;

                while (currentDate <= segment.EndDate)
                {
                    var forecast = await _weatherRepo
                        .GetAsync(segment.LocationId, currentDate);

                    var usedPoiIds = new HashSet<Guid>();
                    var details = new List<ItineraryDetail>();

                    var currentTime = new TimeOnly(8, 0);
                    var endOfDay = new TimeOnly(21, 0);

                    while (currentTime < endOfDay)
                    {
                        var poi = SelectBestPOI(pois, forecast, usedPoiIds);

                        if (poi == null)
                        {
                            currentTime = currentTime.AddMinutes(30);
                            continue;
                        }

                        var endTime = currentTime.Add(DefaultDuration);

                        if (!IsValidTime(poi, currentTime, endTime))
                        {
                            currentTime = currentTime.AddMinutes(30);
                            continue;
                        }

                        var risk = _riskEngine.CalculateRisk(forecast, poi.IsIndoor);

                        details.Add(new ItineraryDetail
                        {
                            DetailId = Guid.NewGuid(),
                            ItineraryId = itinerary.ItineraryId,
                            PoiId = poi.Id,
                            VisitDate = currentDate,
                            StartTime = currentTime,
                            EndTime = endTime,
                            WeatherRiskScore = risk
                        });

                        usedPoiIds.Add(poi.Id);

                        currentTime = endTime.Add(Buffer);
                    }

                    await _detailRepo.AddRangeAsync(details);

                    currentDate = currentDate.AddDays(1);
                }
            }
        }

        private bool IsValidTime(POI poi, TimeOnly start, TimeOnly end)
        {
            if (poi.OpenHour == null || poi.CloseHour == null)
                return true;

            return start >= poi.OpenHour && end <= poi.CloseHour;
        }

        //private TimeSpan EstimateDuration(POI poi)
        //{
        //    return poi.PoiPreferences switch
        //    {
        //        "Restaurant" => TimeSpan.FromHours(1),
        //        "Museum" => TimeSpan.FromHours(2),
        //        "Park" => TimeSpan.FromHours(1.5),
        //        _ => TimeSpan.FromHours(1.5)
        //    };
        //}

        private POI? SelectBestPOI(
            List<POI> pois,
            WeatherForecast forecast,
            HashSet<Guid> used)
        {
            var candidates = pois
                .Where(p => !used.Contains(p.Id))
                .ToList();

            if (!candidates.Any()) return null;

            var best = candidates
                .Select(p => new
                {
                    Poi = p,
                    Score = _riskEngine.CalculateRisk(forecast, p.IsIndoor)
                })
                .OrderByDescending(x => x.Score)
                .First();

            used.Add(best.Poi.Id);

            return best.Poi;
        }
    }
}

using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Weather;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly IOpenMeteoService _openMeteoService;
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);
        private static readonly TimeSpan Buffer = TimeSpan.FromMinutes(30);

        public PlannerService(
            IPOIRepository poiRepo,
            IWeatherForecastRepository weatherRepo,
            IItineraryRepository itineraryRepo,
            IItineraryDetailRepository detailRepo,
            IAdaptiveWeatherRiskEngine riskEngine,
            ITripSegmentRepository segmentRepo,
            IOpenMeteoService openMeteoService)
        {
            _poiRepo = poiRepo;
            _weatherRepo = weatherRepo;
            _itineraryRepo = itineraryRepo;
            _detailRepo = detailRepo;
            _riskEngine = riskEngine;
            _segmentRepo = segmentRepo;
            _openMeteoService = openMeteoService;
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
        //        var forecastData = _openMeteoService.GetAsync(segment.LocationId, segment.StartDate);

        //        while (currentDate <= segment.EndDate)
        //        {
        //            var forecast = await _weatherRepo
        //                .GetAsync(segment.LocationId, currentDate);

        //            var usedPoiIds = new HashSet<Guid>();
        //            var details = new List<ItineraryDetail>();

        //            var currentTime = new TimeOnly(8, 0);
        //            var endOfDay = new TimeOnly(21, 0);

        //            while (currentTime < endOfDay)
        //            {
        //                var poi = SelectBestPOI(pois, forecast, usedPoiIds);

        //                if (poi == null)
        //                {
        //                    currentTime = currentTime.AddMinutes(30);
        //                    continue;
        //                }

        //                var endTime = currentTime.Add(DefaultDuration);

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

        //                currentTime = endTime.Add(Buffer);
        //            }

        //            await _detailRepo.AddRangeAsync(details);

        //            currentDate = currentDate.AddDays(1);
        //        }
        //    }
        //}

        public async Task GenerateAsync(Guid tripId)
        {
            var segments = await _segmentRepo.GetByTripIdAsync(tripId);

            if (segments == null || segments.Count <= 1)
                throw new Exception("Not enough segments");

            segments = segments.OrderBy(x => x.OrderIndex).ToList();

            var itineraries = new List<Itinerary>();
            var allDetails = new List<ItineraryDetail>();

            // ❌ skip starting segment
            for (int i = 1; i < segments.Count; i++)
            {
                var segment = segments[i];
                var nextSegment = i < segments.Count - 1 ? segments[i + 1] : null;

                var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);
                if (pois == null || !pois.Any()) continue;

                var itinerary = new Itinerary
                {
                    ItineraryId = Guid.NewGuid(),
                    SegmentId = segment.SegmentId,
                    GeneratedByAI = true
                };

                itineraries.Add(itinerary);

                var totalDays = (segment.EndDate.Day - segment.StartDate.Day) + 1;

                var dates = Enumerable.Range(0, totalDays)
                    .Select(d => segment.StartDate.AddDays(d))
                    .ToList();

                var forecasts = await _weatherRepo
                    .GetByLocationAndDates(segment.LocationId, dates);

                var forecastDict = forecasts.ToDictionary(x => x.ForecastDate);

                foreach (var currentDate in dates)
                {
                    // 🔥 Skip travel day
                    bool isTravelDay = nextSegment != null &&
                                       currentDate == segment.EndDate &&
                                       nextSegment.StartDate == segment.EndDate;

                    if (isTravelDay)
                        continue;


                    if (!forecastDict.TryGetValue(currentDate, out var forecast) ||
                        forecast.FetchedAt < DateTime.UtcNow.AddHours(-6))
                    {
                        forecast = await GetOrFetchForecastAsync(segment.LocationId, currentDate);
                        forecastDict[currentDate] = forecast;
                    }

                    var usedPoiIds = new HashSet<Guid>();
                    var currentTime = new TimeOnly(8, 0);
                    var endOfDay = new TimeOnly(21, 0);

                    int count = 0;

                    while (currentTime < endOfDay && count < 3) // 🔥 đảm bảo ít nhất 3
                    {
                        var poi = SelectBestPOI(pois, forecast, usedPoiIds);

                        if (poi == null)
                        {
                            currentTime = currentTime.AddMinutes(30);
                            continue;
                        }

                        if (poi.LocationId != segment.LocationId)
                            continue;

                        var endTime = currentTime.Add(DefaultDuration);

                        if (!IsValidTime(poi, currentTime, endTime))
                        {
                            currentTime = currentTime.AddMinutes(30);
                            continue;
                        }

                        var risk = _riskEngine.CalculateRisk(forecast, poi.IsIndoor);

                        allDetails.Add(new ItineraryDetail
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
                        count++;
                    }

                    // 🔥 fallback nếu chưa đủ 3 POI
                    if (count < 3)
                    {
                        var extraPois = pois
                            .Where(p => !usedPoiIds.Contains(p.Id))
                            .Take(3 - count);

                        foreach (var poi in extraPois)
                        {
                            allDetails.Add(new ItineraryDetail
                            {
                                DetailId = Guid.NewGuid(),
                                ItineraryId = itinerary.ItineraryId,
                                PoiId = poi.Id,
                                VisitDate = currentDate,
                                StartTime = currentTime,
                                EndTime = currentTime.Add(DefaultDuration),
                                WeatherRiskScore = 0
                            });

                            currentTime = currentTime.Add(DefaultDuration + Buffer);
                        }
                    }
                }
            }

            await _itineraryRepo.AddRangeAsync(itineraries);
            await _detailRepo.AddRangeAsync(allDetails);
        }

        private async Task<WeatherForecast> GetOrFetchForecastAsync(
    Guid locationId,
    DateOnly date)
        {
            var forecast = await _weatherRepo.GetAsync(locationId, date);

            bool needFetch = forecast == null ||
                             forecast.FetchedAt < DateTime.UtcNow.AddHours(-6);

            if (!needFetch)
                return forecast!;

            // 🔥 gọi OpenMeteo
            var newForecast = await _openMeteoService.GetAsync(locationId, date);

            if (newForecast == null)
                throw new Exception("Failed to fetch weather from OpenMeteo");

            // 🔥 map sang entity nếu cần
            newForecast.LocationId = locationId;
            newForecast.ForecastDate = date;
            newForecast.FetchedAt = DateTime.UtcNow;

            // 🔥 upsert (quan trọng)
            await _weatherRepo.UpsertAsync(newForecast);

            return newForecast;
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

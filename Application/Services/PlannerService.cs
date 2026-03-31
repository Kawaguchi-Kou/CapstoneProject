using System.Text;
using System.Text.Json;
using Application.DTOs.AIResponse;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IGeminiService _gemini;

        public PlannerService(
            IPOIRepository poiRepo,
            IWeatherForecastRepository weatherRepo,
            IItineraryRepository itineraryRepo,
            IItineraryDetailRepository detailRepo,
            IAdaptiveWeatherRiskEngine riskEngine,
            ITripSegmentRepository segmentRepo,
            IOpenMeteoService openMeteoService,
            IAuthService authService,
            IUserRepository userRepository)
        {
            _poiRepo = poiRepo;
            _weatherRepo = weatherRepo;
            _itineraryRepo = itineraryRepo;
            _detailRepo = detailRepo;
            _riskEngine = riskEngine;
            _segmentRepo = segmentRepo;
            _openMeteoService = openMeteoService;
            _authService = authService;
            _userRepository = userRepository;
        }

        //public async Task GenerateAsync(Guid tripId)
        //{
        //    var segments = await _segmentRepo.GetByTripIdAsync(tripId);

        //    if (segments == null || segments.Count <= 1)
        //        throw new Exception("Not enough segments");

        //    segments = segments.OrderBy(x => x.OrderIndex).ToList();

        //    var itineraries = new List<Itinerary>();
        //    var allDetails = new List<ItineraryDetail>();

        //    // ❌ skip starting segment
        //    for (int i = 1; i < segments.Count; i++)
        //    {
        //        var segment = segments[i];
        //        var nextSegment = i < segments.Count - 1 ? segments[i + 1] : null;

        //        var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);
        //        if (pois == null || !pois.Any()) continue;

        //        var itinerary = new Itinerary
        //        {
        //            ItineraryId = Guid.NewGuid(),
        //            SegmentId = segment.SegmentId,
        //            GeneratedByAI = true
        //        };

        //        itineraries.Add(itinerary);

        //        var totalDays = (segment.EndDate.Day - segment.StartDate.Day) + 1;

        //        var dates = Enumerable.Range(0, totalDays)
        //            .Select(d => segment.StartDate.AddDays(d))
        //            .ToList();

        //        var forecasts = await _weatherRepo
        //            .GetByLocationAndDates(segment.LocationId, dates);

        //        var forecastDict = forecasts.ToDictionary(x => x.ForecastDate);

        //        foreach (var currentDate in dates)
        //        {
        //            // 🔥 Skip travel day
        //            bool isTravelDay = nextSegment != null &&
        //                               currentDate == segment.EndDate &&
        //                               nextSegment.StartDate == segment.EndDate;

        //            if (isTravelDay)
        //                continue;


        //            if (!forecastDict.TryGetValue(currentDate, out var forecast) ||
        //                forecast.FetchedAt < DateTime.UtcNow.AddHours(-6))
        //            {
        //                forecast = await GetOrFetchForecastAsync(segment.LocationId, currentDate);
        //                forecastDict[currentDate] = forecast;
        //            }

        //            var usedPoiIds = new HashSet<Guid>();
        //            var currentTime = new TimeOnly(8, 0);
        //            var endOfDay = new TimeOnly(21, 0);

        //            int count = 0;

        //            while (currentTime < endOfDay && count < 3) // 🔥 đảm bảo ít nhất 3
        //            {
        //                var poi = SelectBestPOI(pois, forecast, usedPoiIds);

        //                if (poi == null)
        //                {
        //                    currentTime = currentTime.AddMinutes(30);
        //                    continue;
        //                }

        //                if (poi.LocationId != segment.LocationId)
        //                    continue;

        //                var endTime = currentTime.Add(DefaultDuration);

        //                if (!IsValidTime(poi, currentTime, endTime))
        //                {
        //                    currentTime = currentTime.AddMinutes(30);
        //                    continue;
        //                }

        //                var risk = _riskEngine.CalculateRisk(forecast, poi.IsIndoor);

        //                allDetails.Add(new ItineraryDetail
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
        //                count++;
        //            }

        //            // 🔥 fallback nếu chưa đủ 3 POI
        //            if (count < 3)
        //            {
        //                var extraPois = pois
        //                    .Where(p => !usedPoiIds.Contains(p.Id))
        //                    .Take(3 - count);

        //                foreach (var poi in extraPois)
        //                {
        //                    allDetails.Add(new ItineraryDetail
        //                    {
        //                        DetailId = Guid.NewGuid(),
        //                        ItineraryId = itinerary.ItineraryId,
        //                        PoiId = poi.Id,
        //                        VisitDate = currentDate,
        //                        StartTime = currentTime,
        //                        EndTime = currentTime.Add(DefaultDuration),
        //                        WeatherRiskScore = 0
        //                    });

        //                    currentTime = currentTime.Add(DefaultDuration + Buffer);
        //                }
        //            }
        //        }
        //    }

        //    await _itineraryRepo.AddRangeAsync(itineraries);
        //    await _detailRepo.AddRangeAsync(allDetails);
        //}

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

        public async Task GenerateAsync(Guid tripId)
        {
            //try
            //{
                var segments = await _segmentRepo.GetByTripIdAsync(tripId);

                if (segments == null || segments.Count <= 1)
                    throw new Exception("Not enough segments");

                // 🔥 ORDER FIRST
                segments = segments.OrderBy(x => x.OrderIndex).ToList();

                // 🔥 MAP FOR FAST LOOKUP
                var segmentMap = segments.ToDictionary(s => s.OrderIndex);

                var account = await _authService.GetCurrentAccount();

                var preferences = (await _userRepository
                    .GetPreferenceByAccountIdAsync(account.Id))
                    .Select(x => x.Preference.Name)
                    .ToList();

                var itineraries = new List<Itinerary>();
                var allDetails = new List<ItineraryDetail>();

                // 🔥 LOAD DATA
                var poiDict = new Dictionary<Guid, List<POI>>();
                var forecastDict = new Dictionary<Guid, Dictionary<DateOnly, WeatherForecast>>();

                foreach (var segment in segments.Skip(1)) // skip starting point
                {
                    var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);

                    if (pois == null || !pois.Any())
                    {
                        Console.WriteLine($"⚠️ No POI for segment {segment.SegmentId}");
                        continue;
                    }

                    poiDict[segment.SegmentId] = pois;

                    // ✅ FIX DATE RANGE (no .Day bug)
                    var totalDays = (segment.EndDate.ToDateTime(TimeOnly.MinValue)
                                   - segment.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

                    var dates = Enumerable.Range(0, totalDays + 1)
                        .Select(d => segment.StartDate.AddDays(d))
                        .ToList();

                    var forecasts = await _weatherRepo
                        .GetByLocationAndDates(segment.LocationId, dates);

                    if (forecasts == null || !forecasts.Any())
                    {
                        Console.WriteLine($"⚠️ No forecast for segment {segment.SegmentId}");
                        continue;
                    }

                    forecastDict[segment.SegmentId] =
                        forecasts.ToDictionary(x => x.ForecastDate);
                }

                // 🔥 ONE AI CALL
                var aiPlan = await GenerateFullTripPlanWithAI(
                    segments.Skip(1).ToList(),
                    poiDict,
                    forecastDict,
                    preferences
                );

                if (aiPlan?.Segments == null || !aiPlan.Segments.Any())
                    throw new Exception("AI failed");

                // 🔥 MAP AI → DB
                foreach (var segmentPlan in aiPlan.Segments)
                {
                    if (!segmentMap.TryGetValue(segmentPlan.OrderIndex, out var segment))
                    {
                        Console.WriteLine($"❌ Invalid OrderIndex from AI: {segmentPlan.OrderIndex}");
                        continue;
                    }

                    // ✅ SAFE DICTIONARY ACCESS
                    if (!poiDict.TryGetValue(segment.SegmentId, out var pois) ||
                        !forecastDict.TryGetValue(segment.SegmentId, out var forecasts))
                    {
                        Console.WriteLine($"❌ Missing data for segment {segment.SegmentId}");
                        continue;
                    }

                    var itinerary = new Itinerary
                    {
                        ItineraryId = Guid.NewGuid(),
                        SegmentId = segment.SegmentId,
                        GeneratedByAI = true
                    };

                    itineraries.Add(itinerary);

                    foreach (var day in segmentPlan.Days ?? new List<AIDayPlan>())
                    {
                        // ✅ VALIDATE STRUCTURE
                        if (!ValidateDayPlan(day))
                        {
                            GenerateFallbackPlan(pois, itinerary.ItineraryId, day.Date, allDetails);
                            continue;
                        }

                        // ✅ SAFE FORECAST ACCESS
                        if (!forecasts.TryGetValue(day.Date, out var forecast))
                        {
                            Console.WriteLine($"⚠️ Missing forecast for date {day.Date}");
                            continue;
                        }

                        foreach (var item in day.Plan ?? new List<AIItem>())
                        {
                            var poi = pois.FirstOrDefault(p =>
                                p.Name.Equals(item.Poi, StringComparison.OrdinalIgnoreCase));

                            if (poi == null)
                            {
                                Console.WriteLine($"⚠️ POI not found: {item.Poi}");
                                continue;
                            }

                            // ✅ VALIDATE TYPE
                            if (!IsValidType(item.Type, poi.Type))
                            {
                                Console.WriteLine($"⚠️ Invalid type match: {item.Type} - {poi.Type}");
                                continue;
                            }

                            var (start, end) = ParseTime(item.Time);

                            allDetails.Add(new ItineraryDetail
                            {
                                DetailId = Guid.NewGuid(),
                                ItineraryId = itinerary.ItineraryId,
                                PoiId = poi.Id,
                                VisitDate = day.Date,
                                StartTime = start,
                                EndTime = end,
                                WeatherRiskScore = _riskEngine.CalculateRisk(forecast, poi.IsIndoor)
                            });
                        }
                    }
                }

                // ❗ OPTIONAL: tránh save rỗng
                if (!itineraries.Any() || !allDetails.Any())
                    throw new Exception("No valid itinerary generated");

                // 🔥 SAVE ONCE
                await _itineraryRepo.AddRangeAsync(itineraries);
                await _detailRepo.AddRangeAsync(allDetails);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("❌ Planner ERROR: " + ex);
            //    throw;
            //}
        }


        private async Task<FullTripAIResponse?> GenerateFullTripPlanWithAI(
        List<TripSegment> segments,
        Dictionary<Guid, List<POI>> poiDict,
        Dictionary<Guid, Dictionary<DateOnly, WeatherForecast>> forecastDict,
        List<string> preferences)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are a travel planner AI.");

            prompt.AppendLine("\nUser preferences:");
            foreach (var p in preferences)
                prompt.AppendLine($"- {p}");

            prompt.AppendLine("\nSEGMENTS:");

            foreach (var s in segments)
            {
                prompt.AppendLine($"\nOrderIndex: {s.OrderIndex}");
                prompt.AppendLine($"Dates: {s.StartDate} → {s.EndDate}");

                prompt.AppendLine("POIs:");
                foreach (var poi in poiDict[s.SegmentId].Take(15))
                {
                    var type = poi.Type == POIType.Restaurant ? "restaurant" : "attraction";
                    var indoor = poi.IsIndoor ? "indoor" : "outdoor";

                    prompt.AppendLine($"- {poi.Name} ({type}, {indoor})");
                }

                prompt.AppendLine("Weather:");
                foreach (var w in forecastDict[s.SegmentId])
                {
                    prompt.AppendLine($"{w.Key}: rain {w.Value.PrecipitationProbability}%");
                }
            }

            prompt.AppendLine(@"
                Return JSON ONLY:

                {
                  ""segments"": [
                    {
                      ""segmentId"": ""GUID"",
                      ""days"": [
                        {
                          ""date"": ""2026-03-30"",
                          ""plan"": [
                            { ""type"": ""Breakfast"", ""poi"": ""..."", ""time"": ""07:30-08:30"" },
                            { ""type"": ""Activity"", ""poi"": ""..."", ""time"": ""09:00-11:00"" },
                            { ""type"": ""Lunch"", ""poi"": ""..."", ""time"": ""12:00-13:00"" },
                            { ""type"": ""Activity"", ""poi"": ""..."", ""time"": ""14:00-16:00"" },
                            { ""type"": ""Dinner"", ""poi"": ""..."", ""time"": ""18:00-19:30"" }
                          ]
                        }
                      ]
                    }
                  ]
                }

                Rules:
                - Each segment MUST use the correct OrderIndex provided
                - DO NOT invent new OrderIndex
                - MUST include Breakfast, Lunch, Dinner
                - Breakfast/Lunch/Dinner MUST be restaurant POIs
                - Activities MUST be attraction POIs
                - At least 2 activities per day
                - Avoid outdoor if rain > 60%
                - Use ONLY given POIs
                ");

            var raw = await _gemini.GenerateAsync(prompt.ToString());
            return ParseFullTripResponse(raw);
        }

        private FullTripAIResponse? ParseFullTripResponse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return JsonSerializer.Deserialize<FullTripAIResponse>(text!);
            }
            catch
            {
                return null;
            }
        }

        private bool ValidateDayPlan(AIDayPlan day)
        {
            return day.Plan.Any(p => p.Type == "Breakfast") &&
                   day.Plan.Any(p => p.Type == "Lunch") &&
                   day.Plan.Any(p => p.Type == "Dinner");
        }

        private bool IsValidType(string type, POIType poiType)
        {
            if ((type == "Breakfast" || type == "Lunch" || type == "Dinner")
                && poiType != POIType.Restaurant)
                return false;

            if (type == "Activity" && poiType != POIType.Attraction)
                return false;

            return true;
        }

        private (TimeOnly, TimeOnly) ParseTime(string time)
        {
            var parts = time.Split('-');

            return (
                TimeOnly.Parse(parts[0]),
                TimeOnly.Parse(parts[1])
            );
        }

        private void GenerateFallbackPlan(
            List<POI> pois,
            Guid itineraryId,
            DateOnly date,
            List<ItineraryDetail> details)
        {
            var food = pois.Where(p => p.PoiPreferences.Any(x => x.Preference.Name == "Food")).ToList();
            var attractions = pois.Where(p => !food.Contains(p)).ToList();

            var plan = new[]
            {
                new { Type = "Breakfast", Time = "07:30-08:30", Poi = food.FirstOrDefault() },
                new { Type = "Activity", Time = "09:00-11:00", Poi = attractions.FirstOrDefault() },
                new { Type = "Lunch", Time = "12:00-13:00", Poi = food.Skip(1).FirstOrDefault() },
                new { Type = "Activity", Time = "14:00-16:00", Poi = attractions.Skip(1).FirstOrDefault() },
                new { Type = "Dinner", Time = "18:00-19:30", Poi = food.Skip(2).FirstOrDefault() }
            };

            foreach (var item in plan.Where(p => p.Poi != null))
            {
                var (start, end) = ParseTime(item.Time);

                details.Add(new ItineraryDetail
                {
                    DetailId = Guid.NewGuid(),
                    ItineraryId = itineraryId,
                    PoiId = item.Poi!.Id,
                    VisitDate = date,
                    StartTime = start,
                    EndTime = end,
                    WeatherRiskScore = 0
                });
            }
        }
    }
}

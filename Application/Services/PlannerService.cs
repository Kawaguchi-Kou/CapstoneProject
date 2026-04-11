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
        private readonly IWeatherService _weatherService;
        private readonly IPOIService _poiService;

        public PlannerService(
            IPOIRepository poiRepo,
            IWeatherForecastRepository weatherRepo,
            IItineraryRepository itineraryRepo,
            IItineraryDetailRepository detailRepo,
            IAdaptiveWeatherRiskEngine riskEngine,
            ITripSegmentRepository segmentRepo,
            IOpenMeteoService openMeteoService,
            IAuthService authService,
            IUserRepository userRepository,
            IWeatherService weatherService,
            IGeminiService gemini,
            IPOIService poiService)
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
            _weatherService = weatherService;
            _gemini = gemini;
            _poiService = poiService;
        }

        public async Task GenerateAsync(Guid tripId)
        {
            // 🔥 1. Load segments
            var segments = await _segmentRepo.GetByTripIdAsync(tripId);

            if (segments == null || segments.Count <= 1)
                throw new Exception("Not enough segments");

            segments = segments.OrderBy(x => x.OrderIndex).ToList();

            var account = await _authService.GetCurrentAccount();

            var preferences = (await _userRepository
                .GetPreferenceByAccountIdAsync(account.Id))
                .Select(x => x.Preference.Name)
                .ToList();

            var itineraries = new List<Itinerary>();
            var allDetails = new List<ItineraryDetail>();

            // 🚀 3. LOOP THROUGH SEGMENTS (START FROM 1)
            for (int i = 1; i < segments.Count; i++)
            {
                var segment = segments[i];
                var prevLocationId = segments[i - 1].LocationId;

                bool isDrivingDay = segment.StartDate.Date == segment.EndDate.Date;

                // 🔥 4. LOAD POIs (A + B)
                var pois = new List<POI>();

                var startPois = await _poiService.GetPoisByLocationSortedByPreferenceAsync(account.Id, prevLocationId);
                var endPois = await _poiService.GetPoisByLocationSortedByPreferenceAsync(account.Id, segment.LocationId);

                pois.AddRange(startPois.Select(x => new POI
                {
                    Id = x.Id,
                    Name = x.Name,
                    Address = x.Address,
                    IsIndoor = x.IsIndoor,
                    Location = x.Location
                }));

                pois.AddRange(endPois.Select(x => new POI
                {
                    Id = x.Id,
                    Name = x.Name,
                    Address = x.Address,
                    IsIndoor = x.IsIndoor,
                    Location = x.Location
                }));

                if (!pois.Any())
                    continue;

                // 🔥 5. BUILD DATES
                var totalDays = (segment.EndDate - segment.StartDate).Days;

                var dates = Enumerable.Range(0, totalDays + 1)
                    .Select(d => segment.StartDate.AddDays(d))
                    .ToList();

                // 🔥 6. LOAD WEATHER
                Dictionary<DateTime, WeatherForecast> forecasts;

                try
                {
                    forecasts = await _weatherService
                        .GetRangeOptimizedAsync(segment.LocationId, dates);
                }
                catch
                {
                    forecasts = new Dictionary<DateTime, WeatherForecast>();
                }

                // 🔥 7. CALL AI (SAFE)
                var aiResult = await GenerateSegmentPlanSafeAsync(
                    segment,
                    prevLocationId,
                    pois,
                    forecasts,
                    preferences
                );

                // 🔥 8. CREATE ITINERARY
                var itinerary = new Itinerary
                {
                    ItineraryId = Guid.NewGuid(),
                    SegmentId = segment.SegmentId,
                    GeneratedByAI = aiResult != null
                };

                itineraries.Add(itinerary);

                // 🚨 9. FALLBACK ENTIRE SEGMENT
                if (aiResult == null || aiResult.Days == null || !aiResult.Days.Any())
                {
                    foreach (var date in dates)
                    {
                        await GenerateFallbackPlanAsync(
                            pois,
                            itinerary.ItineraryId,
                            date,
                            segment.LocationId,
                            forecasts,
                            allDetails,
                            isDrivingDay
                        );
                    }

                    continue;
                }

                // 🔥 10. MAP AI RESULT
                foreach (var day in aiResult.Days)
                {
                    if (!ValidateDayPlan(day, isDrivingDay))
                    {
                        await GenerateFallbackPlanAsync(
                            pois,
                            itinerary.ItineraryId,
                            day.Date,
                            segment.LocationId,
                            forecasts,
                            allDetails,
                            isDrivingDay
                        );
                        continue;
                    }

                    var normalizedPlan = NormalizePlan(day.Plan, isDrivingDay);

                    var forecast = await EnsureForecastAsync(
                        segment.LocationId,
                        day.Date,
                        forecasts
                    );

                    foreach (var item in normalizedPlan)
                    {
                        var poi = pois.FirstOrDefault(p =>
                            p.Name.Contains(item.Poi, StringComparison.OrdinalIgnoreCase) ||
                            item.Poi.Contains(p.Name, StringComparison.OrdinalIgnoreCase));

                        if (poi == null) continue;

                        if (!IsValidType(item.Type, poi.Type))
                            continue;

                        var (start, end) = ParseTime(item.Time);

                        var risk = forecast != null
                            ? _riskEngine.CalculateRisk(forecast, poi.IsIndoor)
                            : 0;

                        allDetails.Add(new ItineraryDetail
                        {
                            DetailId = Guid.NewGuid(),
                            ItineraryId = itinerary.ItineraryId,
                            PoiId = poi.Id,
                            VisitDate = day.Date,
                            StartTime = start,
                            EndTime = end,
                            WeatherRiskScore = risk
                        });
                    }
                }
            }

            if (!itineraries.Any() || !allDetails.Any())
                throw new Exception("No valid itinerary generated");

            await _itineraryRepo.AddRangeAsync(itineraries);
            await _detailRepo.AddRangeAsync(allDetails);
        }

        private async Task<SegmentAIResponse?> GenerateSegmentPlanSafeAsync(
            TripSegment segment,
            Guid prevLocationId,
            List<POI> pois,
            Dictionary<DateTime, WeatherForecast> forecasts,
            List<string> preferences)
        {
            try
            {
                return await GenerateSegmentPlanAsync(segment, pois, forecasts, preferences);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 AI FAIL SEGMENT {segment.OrderIndex}");
                Console.WriteLine(ex.Message);
                return null; // fallback trigger
            }
        }

        private bool IsDrivingDay(TripSegment segment)
        {
            return segment.StartDate.Date == segment.EndDate.Date;
        }

        private async Task<SegmentAIResponse?> GenerateSegmentPlanAsync(
    TripSegment segment,
    List<POI> pois,
    Dictionary<DateTime, WeatherForecast> forecasts,
    List<string> preferences)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are a travel planner AI.");
            prompt.AppendLine($"This is a trip from Location A to Location B.");
            prompt.AppendLine("\nUser Preferences:");
            foreach (var p in preferences.Take(5))
                prompt.AppendLine($"- {p}");

            prompt.AppendLine($"\nDates: {segment.StartDate:yyyy-MM-dd} → {segment.EndDate:yyyy-MM-dd}");

            var isDrivingDay = IsDrivingDay(segment);

            if (isDrivingDay)
            {
                prompt.AppendLine("\nNOTE: This is a DRIVING DAY. Keep plan light.");
            }

            // 🔥 limit POIs
            var topPois = pois.Take(12).ToList();

            prompt.AppendLine("\nPOIs:");
            foreach (var poi in topPois)
            {
                var type = poi.Type == POIType.Restaurant ? "restaurant" : "attraction";
                prompt.AppendLine($"- {poi.Name} ({type})");
            }

            // 🔥 limit weather
            prompt.AppendLine("\nWeather:");
            foreach (var w in forecasts.Take(5))
            {
                prompt.AppendLine($"{w.Key:yyyy-MM-dd}: rain {w.Value.PrecipitationProbability}%");
            }

            prompt.AppendLine(@"
            Return JSON:
            {
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

            STRICT RULES:
                NORMAL DAY:
                - Follow this structure:
                  Breakfast → Activity → Lunch → Activity → Dinner
                - Include at least 2 activities
                - Include at least 1 activity between meals

                DRIVING DAY (StartDate == EndDate):
                - Only include:
                  Breakfast → Lunch → Dinner
                - NO activities
                - Keep schedule light
                -This is a driving segment from Location A to Location B.
                - Breakfast should be near the start location
                - Lunch can be along the route
                - Dinner should be near the destinationUse POIs accordingly.

                GENERAL RULES:

                - Meals (Breakfast, Lunch, Dinner):
                  MUST use POIs with type:
                  Restaurant, Cafe, or StreetFood

                - Activities:
                  MUST use POIs with type:
                  Attraction, Landmark, Viewpoint, Beach,
                  Museum, CulturalSite, HistoricalSite,
                  Park, Nature, Waterfall,
                  Shopping, Market, NightMarket,
                  Entertainment

                - DO NOT use Hotel or Transportation as activities

                - Use ONLY provided POIs
                - Prefer indoor places if rain > 60%
                - Keep times exactly as provided
                - Do not invent POIs
             ");
            prompt.AppendLine("If not enough valid POIs, return best possible plan.");

            var raw = await RetryGeminiAsync(prompt.ToString());
            return ParseSegmentResponse(raw);
        }

        private async Task<string> RetryGeminiAsync(string prompt)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await _gemini.GenerateAsync(prompt);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("503"))
                {
                    await Task.Delay(2000 * (i + 1));
                }
            }

            throw new Exception("Gemini failed after retries");
        }

        private SegmentAIResponse? ParseSegmentResponse(string raw)
        {
            try
            {
                var cleanJson = raw.Trim();

                if (cleanJson.StartsWith("```json"))
                {
                    cleanJson = cleanJson.Substring(7).TrimEnd('`');
                }

                return JsonSerializer.Deserialize<SegmentAIResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private bool ValidateDayPlan(AIDayPlan day, bool isDrivingDay)
        {
            var types = day.Plan.Select(p => p.Type).ToList();

            // ✅ đủ meal
            if (!types.Contains("Breakfast") ||
                !types.Contains("Lunch") ||
                !types.Contains("Dinner"))
                return false;

            var activityCount = types.Count(t => t == "Activity");

            // 🚗 driving day
            if (isDrivingDay)
                return activityCount == 0;

            // 🧭 normal day
            return activityCount >= 2;
        }

        private List<AIItem> NormalizePlan(List<AIItem> plan, bool isDrivingDay)
        {
            if (isDrivingDay)
            {
                return plan
                    .Where(p => p.Type == "Breakfast" ||
                                p.Type == "Lunch" ||
                                p.Type == "Dinner")
                    .ToList();
            }

            // normal day
            var orderedTypes = new[]
            {
        "Breakfast",
        "Activity",
        "Lunch",
        "Activity",
        "Dinner"
    };

            var result = new List<AIItem>();

            foreach (var type in orderedTypes)
            {
                var item = plan.FirstOrDefault(p => p.Type == type);
                if (item != null)
                    result.Add(item);
            }

            return result;
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

        private async Task GenerateFallbackPlanAsync(
    List<POI> pois,
    Guid itineraryId,
    DateTime date,
    Guid locationId,
    Dictionary<DateTime, WeatherForecast> forecasts,
    List<ItineraryDetail> details,
    bool isDrivingDay)
        {
            var mealPois = pois.Where(p =>
                p.Type == POIType.Restaurant ||
                p.Type == POIType.Cafe ||
                p.Type == POIType.StreetFood
            ).ToList();

            var activityPois = pois.Where(p =>
                p.Type != POIType.Hotel &&
                p.Type != POIType.Resort 
            ).ToList();

            // 🔥 Ensure forecast
            var forecast = await EnsureForecastAsync(locationId, date, forecasts);

            // 🚗 DRIVING DAY
            if (isDrivingDay)
            {
                var plan = new[]
                {
            new { Type = "Breakfast", Time = "07:30-08:30", Poi = mealPois.FirstOrDefault() },
            new { Type = "Lunch", Time = "12:00-13:00", Poi = mealPois.Skip(1).FirstOrDefault() ?? mealPois.FirstOrDefault() },
            new { Type = "Dinner", Time = "18:00-19:30", Poi = mealPois.Skip(2).FirstOrDefault() ?? mealPois.FirstOrDefault() }
        };

                foreach (var item in plan.Where(p => p.Poi != null))
                {
                    var (start, end) = ParseTime(item.Time);

                    var risk = forecast != null
                        ? _riskEngine.CalculateRisk(forecast, item.Poi!.IsIndoor)
                        : 0;

                    details.Add(new ItineraryDetail
                    {
                        DetailId = Guid.NewGuid(),
                        ItineraryId = itineraryId,
                        PoiId = item.Poi!.Id,
                        VisitDate = date,
                        StartTime = start,
                        EndTime = end,
                        WeatherRiskScore = risk
                    });
                }

                return;
            }

            // 🧭 NORMAL DAY
            var planNormal = new[]
            {
        new { Type = "Breakfast", Time = "07:30-08:30", Poi = mealPois.FirstOrDefault() },
        new { Type = "Activity", Time = "09:00-11:00", Poi = activityPois.FirstOrDefault() },
        new { Type = "Lunch", Time = "12:00-13:00", Poi = mealPois.Skip(1).FirstOrDefault() ?? mealPois.FirstOrDefault() },
        new { Type = "Activity", Time = "14:00-16:00", Poi = activityPois.Skip(1).FirstOrDefault() ?? activityPois.FirstOrDefault() },
        new { Type = "Dinner", Time = "18:00-19:30", Poi = mealPois.Skip(2).FirstOrDefault() ?? mealPois.FirstOrDefault() }
    };

            foreach (var item in planNormal.Where(p => p.Poi != null))
            {
                var (start, end) = ParseTime(item.Time);

                var risk = forecast != null
                    ? _riskEngine.CalculateRisk(forecast, item.Poi!.IsIndoor)
                    : 0;

                details.Add(new ItineraryDetail
                {
                    DetailId = Guid.NewGuid(),
                    ItineraryId = itineraryId,
                    PoiId = item.Poi!.Id,
                    VisitDate = date,
                    StartTime = start,
                    EndTime = end,
                    WeatherRiskScore = risk
                });
            }
        }

        private async Task<WeatherForecast?> EnsureForecastAsync(
    Guid locationId,
    DateTime date,
    Dictionary<DateTime, WeatherForecast> forecasts)
        {
            // 🔥 1. Try cache (from WeatherService)
            if (forecasts != null && forecasts.TryGetValue(date.Date, out var cached))
                return cached;

            try
            {
                // 🔥 2. Fetch from OpenMeteo
                var forecast = await _openMeteoService.GetAsync(locationId, date);

                if (forecast == null)
                    return null;

                // 🔥 3. Enrich entity
                forecast.LocationId = locationId;
                forecast.ForecastDate = date.Date;
                forecast.FetchedAt = DateTime.UtcNow;

                // 🔥 4. Save to DB (UPSERT)
                await _weatherRepo.UpsertAsync(forecast);

                // 🔥 5. Add back to dictionary (cache for current run)
                if (forecasts != null)
                {
                    forecasts[date.Date] = forecast;
                }

                return forecast;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 EnsureForecastAsync FAILED:");
                Console.WriteLine(ex.Message);

                return null; // ⚠️ caller must handle fallback risk = 0
            }
        }
    }
}

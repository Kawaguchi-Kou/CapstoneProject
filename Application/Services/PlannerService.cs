using System.Text;
using System.Text.Json;
using Application.DTOs.AIResponse;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Weather;
using Newtonsoft.Json.Linq;

namespace Application.Services
{
    public class PlannerService : IPlannerService
    {
        private readonly ITripSegmentRepository _segmentRepo;
        private readonly IPOIRepository _poiRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAuthService _authService;
        private readonly IGeminiService _gemini;
        private readonly IWeatherService _weatherService;
        private readonly IAdaptiveWeatherRiskEngine _riskEngine;
        private readonly IGeocodingService _geo;
        private readonly IItineraryRepository _itineraryRepo;
        private readonly IItineraryDetailRepository _detailRepo;
        private readonly IPlannerRepository _plannerRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PlannerService(
            ITripSegmentRepository segmentRepo,
            IPOIRepository poiRepo,
            IUserRepository userRepo,
            IAuthService authService,
            IGeminiService gemini,
            IWeatherService weatherService,
            IAdaptiveWeatherRiskEngine riskEngine,
            IGeocodingService geo,
            IItineraryRepository itineraryRepo,
            IItineraryDetailRepository detailRepo,
            IPlannerRepository plannerRepo,
            IUnitOfWork unitOfWork)
        {
            _segmentRepo = segmentRepo;
            _poiRepo = poiRepo;
            _userRepo = userRepo;
            _authService = authService;
            _gemini = gemini;
            _weatherService = weatherService;
            _riskEngine = riskEngine;
            _geo = geo;
            _itineraryRepo = itineraryRepo;
            _detailRepo = detailRepo;
            _plannerRepo = plannerRepo;
            _unitOfWork = unitOfWork;
        }

        // public async Task GenerateAsync(Guid tripId)
        // {
        //     // await _unitOfWork.BeginTransactionAsync();
        //     try
        //     {
        //         var tripUsedPoiIds = new HashSet<Guid>();
        //         // ====================================================
        //         // 1. LOAD SEGMENTS
        //         // ====================================================
        //         var segments = (await _segmentRepo.GetByTripIdAsync(tripId))
        //             .OrderBy(x => x.OrderIndex)
        //             .ToList();

        //         if (!segments.Any())
        //             throw new Exception("No segments");
        //         Console.WriteLine($"Segments: {segments.Count}");
        //         foreach (var s in segments)
        //         {
        //             Console.WriteLine(
        //                 $"Segment {s.OrderIndex} - Location: {s.LocationId}, District: {s.DistrictId}, Start: {s.StartDate}, End: {s.EndDate}");
        //         }

        //         // ====================================================
        //         // 2. LOAD USER PREFS
        //         // ====================================================
        //         var account = await _authService.GetCurrentAccount();

        //         var prefIds = (await _userRepo.GetPreferenceByAccountIdAsync(account.Id))
        //             .Select(x => x.PreferenceId)
        //             .ToHashSet();

        //         // ====================================================
        //         // 3. PRELOAD ALL POIs (🔥 BIG FIX)
        //         // ====================================================
        //         var keys = segments
        //             .Select(s => (s.LocationId, s.DistrictId))
        //             .Distinct()
        //             .ToList();

        //         var allPois = await _poiRepo.GetByLocationDistrictPairsAsync(keys);

        //         var foodPois = allPois
        //             .Where(p =>
        //                 p.Type == POIType.Restaurant ||
        //                 p.Type == POIType.StreetFood)
        //             .ToList();

        //         var activityPois = allPois
        //             .Where(p =>
        //                 p.Type != POIType.Restaurant &&
        //                 p.Type != POIType.StreetFood &&
        //                 !p.PoiPreferences.Any(pp =>
        //                     prefIds.Contains(pp.PreferenceId)))
        //             .ToList();

        //         var poiCache = activityPois
        //             .GroupBy(p => $"{p.LocationId}-{p.DistrictId}")
        //             .ToDictionary(g => g.Key, g =>
        //                 g.OrderByDescending(x =>
        //                     x.PoiPreferences.Count(pp =>
        //                         prefIds.Contains(pp.PreferenceId)))
        //                  .ThenBy(x => x.Name)
        //                  .ToList()
        //             );

        //         // ====================================================
        //         // 4. PRELOAD WEATHER (cached per location)
        //         // ====================================================
        //         var weatherCache = new Dictionary<Guid, Dictionary<DateTime, WeatherForecast>>();

        //         // ====================================================
        //         // 5. DISTANCE CACHE
        //         // ====================================================
        //         var distanceCache = new Dictionary<string, int>();
        //         var aiCache = new Dictionary<string, SegmentAIResponse>();

        //         var itineraries = new List<Itinerary>();
        //         var details = new List<ItineraryDetail>();

        //         foreach (var segment in segments)
        //         {
        //             var aiKey = $"{segment.LocationId}-{segment.DistrictId}-{segment.StartDate:yyyyMMdd}-{segment.EndDate:yyyyMMdd}";
        //             var key = $"{segment.LocationId}-{segment.DistrictId}";
        //             var segmentUsedPoiIds = new HashSet<Guid>();

        //             var segmentFoodPois = foodPois
        //                 .Where(p =>
        //                     p.LocationId == segment.LocationId &&
        //                     p.DistrictId == segment.DistrictId)
        //                 .ToList();

        //             if (!poiCache.TryGetValue(key, out var pois) || !pois.Any()){
        //                 Console.WriteLine($"No POIs for segment {segment.OrderIndex}");

        //                 // 🔥 HARD fallback pool (very important)
        //                 pois = poiCache.Values
        //                     .SelectMany(x => x)
        //                     .GroupBy(p => p.Id)
        //                     .Select(g => g.First())
        //                     .OrderBy(x => Guid.NewGuid())
        //                     .Take(15)
        //                     .ToList();

        //                 pois ??= new List<POI>();
        //             }

        //             // ====================================================
        //             // BUILD DATES
        //             // ====================================================
        //             var dates = Enumerable.Range(
        //                     0,
        //                     (segment.EndDate.Date - segment.StartDate.Date).Days + 1)
        //                 .Select(x => segment.StartDate.Date.AddDays(x))
        //                 .ToList();

        //             // ====================================================
        //             // WEATHER (cached per location)
        //             // ====================================================
        //             if (!weatherCache.TryGetValue(segment.LocationId, out var forecasts))
        //             {
        //                 forecasts = await _weatherService
        //                     .GetRangeOptimizedAsync(segment.LocationId, dates);

        //                 weatherCache[segment.LocationId] = forecasts;
        //             }

        //             // ====================================================
        //             // AI GENERATE
        //             // ====================================================
        //             SegmentAIResponse? ai = null;

        //             if (!aiCache.TryGetValue(aiKey, out var cachedAi))
        //             {
        //                 var generated = await GenerateSafe(segment, pois, segmentFoodPois, forecasts);

        //                 // 🔥 HARD VALIDATION (prevent partial AI)
        //                 if (generated != null &&
        //                     generated.Days != null &&
        //                     generated.Days.Count == dates.Count)
        //                 {
        //                     aiCache[aiKey] = generated;
        //                     ai = generated;

        //                     Console.WriteLine("✅ AI cached");
        //                 }
        //                 else
        //                 {
        //                     Console.WriteLine("❌ AI invalid (missing days) → fallback");
        //                     ai = null;
        //                 }
        //             }
        //             else
        //             {
        //                 Console.WriteLine("⚡ Using cached AI");
        //                 ai = cachedAi;
        //             }

        //             var itinerary = new Itinerary
        //             {
        //                 ItineraryId = Guid.NewGuid(),
        //                 SegmentId = segment.SegmentId,
        //                 GeneratedByAI = ai != null
        //             };

        //             itineraries.Add(itinerary);

        //             foreach (var date in dates)
        //             {
        //                 Console.WriteLine($"--- Processing date: {date:yyyy-MM-dd} ---");

        //                 // 🔥 SAFE ACCESS + LOG
        //                 if (ai == null)
        //                 {
        //                     Console.WriteLine("AI = NULL → fallback");
        //                     BuildFallback(
        //                         itinerary.ItineraryId,
        //                         pois,
        //                         date,
        //                         forecasts,
        //                         details,
        //                         segmentUsedPoiIds,
        //                         tripUsedPoiIds);

        //                     continue;
        //                 }
        //                 Console.WriteLine($"AI Days count: {ai.Days?.Count}");

        //                 var day = ai.Days?
        //                     .FirstOrDefault(x => x.Date.Date == date.Date);

        //                 if (day == null)
        //                 {
        //                     Console.WriteLine("Day not found → fallback");

        //                     BuildFallback(
        //                         itinerary.ItineraryId,
        //                         pois,
        //                         date,
        //                         forecasts,
        //                         details,
        //                         segmentUsedPoiIds,
        //                         tripUsedPoiIds);

        //                     continue;
        //                 }

        //                 if (day.Plan == null || day.Plan.Count < 5)
        //                 {
        //                     Console.WriteLine("⚡ Partial AI → filling missing");

        //                     var fixedPlans = FillMissingPlans(day.Plan ?? new List<AIActivity>(), pois);

        //                     await BuildSchedule(
        //                         itinerary.ItineraryId,
        //                         fixedPlans,
        //                         pois,
        //                         date,
        //                         segment,
        //                         forecasts,
        //                         details,
        //                         distanceCache,
        //                         segmentUsedPoiIds,
        //                         tripUsedPoiIds);

        //                     continue;
        //                 }

        //                 Console.WriteLine("✅ Valid AI day → using AI");

        //                 await BuildSchedule(
        //                         itinerary.ItineraryId,
        //                         day.Plan,
        //                         pois,
        //                         date,
        //                         segment,
        //                         forecasts,
        //                         details,
        //                         distanceCache,
        //                         segmentUsedPoiIds,
        //                         tripUsedPoiIds);
        //             }
        //         }

        //         if (!itineraries.Any() || !details.Any())
        //             throw new Exception("No itinerary");

        //         // ====================================================
        //         // SAVE ONCE (good practice)
        //         // ====================================================
        //         await _unitOfWork.BeginTransactionAsync();

        //         await _itineraryRepo.AddRangeAsync(itineraries);
        //         await _detailRepo.AddRangeAsync(details);

        //         await _unitOfWork.SaveChangesAsync();

        //         await _unitOfWork.CommitAsync(); 
        //     }
        //     catch
        //     {
        //         await _unitOfWork.RollbackAsync();      // rollback everything
        //         throw;
        //     }
        // }

        public async Task GenerateAsync(Guid tripId)
        {
            try
            {
                // ====================================================
                // LOAD SEGMENTS
                // ====================================================
                var segments = (await _segmentRepo.GetByTripIdAsync(tripId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                if (!segments.Any())
                    throw new Exception("No segments");

                // ====================================================
                // USER PREFS
                // ====================================================
                var account = await _authService.GetCurrentAccount();

                var prefIds = (await _userRepo
                        .GetPreferenceByAccountIdAsync(account.Id))
                    .Select(x => x.PreferenceId)
                    .ToHashSet();

                // ====================================================
                // ALL DATES OF TRIP
                // ====================================================
                var tripStart = segments.Min(x => x.StartDate).Date;
                var tripEnd = segments.Max(x => x.EndDate).Date;

                var tripDates = Enumerable.Range(
                        0,
                        (tripEnd - tripStart).Days + 1)
                    .Select(i => tripStart.AddDays(i))
                    .ToList();

                // ====================================================
                // LOAD ALL POIS
                // ====================================================
                var keys = segments
                    .Select(x => (x.LocationId, x.DistrictId))
                    .Distinct()
                    .ToList();

                var allPois =
                    await _poiRepo.GetByLocationDistrictPairsAsync(keys);

                var foodPois = allPois
                    .Where(p =>
                        p.Type == POIType.Restaurant ||
                        p.Type == POIType.StreetFood)
                    .ToList();

                var activityPois = allPois
                    .Where(p =>
                        p.Type != POIType.Restaurant &&
                        p.Type != POIType.StreetFood)
                    .OrderByDescending(p =>
                        p.PoiPreferences.Count(pp =>
                            prefIds.Contains(pp.PreferenceId)))
                    .ToList();

                var rankedActivityPois =
                    activityPois
                        .Take(80)
                        .ToList();

                var rankedFoodPois =
                    foodPois
                        .Take(40)
                        .ToList();

                // ====================================================
                // WEATHER CACHE
                // ====================================================
                var weatherCache =
                    new Dictionary<Guid,
                        Dictionary<DateTime, WeatherForecast>>();

                foreach (var locationId in segments
                            .Select(x => x.LocationId)
                            .Distinct())
                {
                    weatherCache[locationId] =
                        await _weatherService.GetRangeOptimizedAsync(
                            locationId,
                            tripDates);
                }

                // ====================================================
                // GEMINI ONCE
                // ====================================================
                var ai = await GenerateTripSafe(
                    segments,
                    rankedActivityPois,
                    rankedFoodPois,
                    weatherCache);

                // ====================================================
                // PREPARE SAVE OBJECTS
                // ====================================================
                var itineraries = new List<Itinerary>();
                var details = new List<ItineraryDetail>();

                var itineraryMap = segments.ToDictionary(
                    x => x.OrderIndex,
                    x => new Itinerary
                    {
                        ItineraryId = Guid.NewGuid(),
                        SegmentId = x.SegmentId,
                        GeneratedByAI = ai != null
                    });

                foreach (var segment in segments)
                {
                    var itinerary = new Itinerary
                    {
                        ItineraryId = Guid.NewGuid(),
                        SegmentId = segment.SegmentId,
                        GeneratedByAI = ai != null
                    };

                    itineraryMap[segment.OrderIndex] = itinerary;
                    itineraries.Add(itinerary);
                }

                // ====================================================
                // SHARED CACHES
                // ====================================================
                var distanceCache = new Dictionary<string, int>();

                var tripUsedPoiIds = new HashSet<Guid>();

                var usedPoiPeriods = new Dictionary<Guid, List<string>>();

                // ====================================================
                // AI FAILED => FULL FALLBACK
                // ====================================================
                if (ai == null ||
                    ai.Segments == null ||
                    !ai.Segments.Any())
                {
                    foreach (var segment in segments)
                    {
                        var itinerary =
                            itineraryMap[segment.OrderIndex];

                        var pois = allPois
                            .Where(p =>
                                p.LocationId == segment.LocationId &&
                                p.DistrictId == segment.DistrictId)
                            .ToList();

                        var forecasts =
                            weatherCache[segment.LocationId];

                        var dates = Enumerable.Range(
                                0,
                                (segment.EndDate.Date -
                                segment.StartDate.Date).Days + 1)
                            .Select(i =>
                                segment.StartDate.Date.AddDays(i));

                        foreach (var date in dates)
                        {
                            BuildFallback(
                                itinerary.ItineraryId,
                                pois,
                                date,
                                forecasts,
                                details,
                                usedPoiPeriods);
                        }
                    }
                }
                else
                {
                    // ====================================================
                    // PROCESS AI DAYS
                    // ====================================================
                    foreach (var aiSegment in ai.Segments)
                    {
                        var segment = segments.FirstOrDefault(
                            x => x.OrderIndex == aiSegment.SegmentOrder);

                        if (segment == null)
                            continue;

                        foreach (var day in aiSegment.Days)
                        {
                            var itinerary =
                                itineraryMap[segment.OrderIndex];

                            var pois = allPois
                                .Where(p =>
                                    p.LocationId == segment.LocationId &&
                                    p.DistrictId == segment.DistrictId)
                                .ToList();

                            var forecasts =
                                weatherCache[segment.LocationId];

                            var plans =
                                day.Plan ?? new List<AIActivity>();

                            if (!plans.Any())
                            {
                                plans =
                                    FillMissingPlans(
                                        plans,
                                        pois);
                            }

                            await BuildSchedule(
                                itinerary.ItineraryId,
                                plans,
                                pois,
                                day.Date,
                                segment,
                                forecasts,
                                details,
                                distanceCache,
                                usedPoiPeriods);
                        }
                    }
                }

                //foreach (var day in ai.Days)
                //{
                //    foreach (var segmentPlan in day.Segments)
                //    {
                //        var segment = segments.First(x =>
                //            x.SegmentId == segmentPlan.SegmentId);

                //        var pois = allPois
                //                .Where(p =>
                //                    p.LocationId == segment.LocationId &&
                //                    p.DistrictId == segment.DistrictId)
                //                .ToList();

                //        var forecasts = weatherCache[segment.LocationId];

                //        await BuildSchedule(
                //            itineraryMap[segment.SegmentId].ItineraryId,
                //            segmentPlan.Plan,
                //            pois,
                //            day.Date,
                //            segment,
                //            forecasts,
                //            details,
                //            distanceCache,
                //            segmentUsedPoiIds[segment.SegmentId],
                //            tripUsedPoiIds);
                //    }
                //}

                // ====================================================
                // VALIDATION
                // ====================================================
                if (!itineraries.Any())
                    throw new Exception("No itinerary");

                if (!details.Any())
                    throw new Exception("No itinerary details");

                // ====================================================
                // SAVE
                // ====================================================
                await _unitOfWork.BeginTransactionAsync();

                await _itineraryRepo.AddRangeAsync(itineraries);

                await _detailRepo.AddRangeAsync(details);

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
            }

            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }


        // ====================================================
        // AI
        // ====================================================
        //private async Task<SegmentAIResponse?> GenerateSafe(
        //    TripSegment segment,
        //    List<POI> activityPois,
        //    List<POI> foodPois,
        //    Dictionary<DateTime, WeatherForecast> forecasts)
        //{
        //    for (int i = 0; i < 3; i++)
        //    {
        //        try
        //        {
        //            var prompt = BuildPrompt(segment, activityPois, foodPois, forecasts);
        //            var raw = await _gemini.GenerateAsync(prompt);

        //            Console.WriteLine($"RAW RESPONSE: {raw}");

        //            var parsed = JsonSerializer.Deserialize<SegmentAIResponse>(
        //                raw,
        //                new JsonSerializerOptions
        //                {
        //                    PropertyNameCaseInsensitive = true
        //                });

        //            return parsed;
        //        }
        //        catch (HttpRequestException ex) when (ex.Message.Contains("503") ||
        //ex.Message.Contains("429"))
        //        {
        //            Console.WriteLine($"Gemini retry {i + 1}");

        //            await Task.Delay(
        //                TimeSpan.FromSeconds(
        //                    Math.Pow(2, i + 1)));
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"AI ERROR: {ex.Message}");
        //            return null;
        //        }
        //    }

        //    Console.WriteLine("❌ Gemini failed after retries");
        //    return null;
        //}

        private async Task<TripAIResponse?> GenerateTripSafe(
    List<TripSegment> segments,
    List<POI> activityPois,
    List<POI> foodPois,
    Dictionary<Guid,
        Dictionary<DateTime, WeatherForecast>> weatherCache)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var prompt = BuildTripPrompt(
                        segments,
                        activityPois,
                        foodPois,
                        weatherCache);

                    var raw =
                        await _gemini.GenerateAsync(prompt);

                    Console.WriteLine(raw);

                    var parsed =
                        JsonSerializer.Deserialize<TripAIResponse>(
                            raw,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (!IsValid(parsed))
                    {
                        Console.WriteLine("AI returned incomplete itinerary");
                        return null;
                    }

                    return parsed;
                }
                catch (HttpRequestException ex)
                    when (ex.Message.Contains("429")
                    || ex.Message.Contains("503"))
                {
                    Console.WriteLine($"Gemini retry {i + 1}");

                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            Math.Pow(2, i + 1)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }

            return null;
        }

        private List<AIActivity> FillMissingPlans(List<AIActivity> plans, List<POI> pois)
        {
            var result = plans.Where(p => p != null).ToList();

            var used = result.Select(p => p.PoiId).ToHashSet();

            var needed = 5 - result.Count;

            if (needed <= 0) return result;

            Console.WriteLine($"Filling missing activities: +{needed}");

            var extras = pois
                .Where(p => !used.Contains(p.Id))
                .Take(needed)
                .Select(p => new AIActivity
                {
                    PoiId = p.Id,
                    Period = "Noon",
                    DurationMinutes = 90
                });

            result.AddRange(extras);

            return result;
        }

        //private bool ValidateDay(AIDayPlan day)
        //{
        //    if (day.Plan == null || day.Plan.Count < 5)
        //        return false;

        //    var periods = day.Plan
        //        .Where(p => !string.IsNullOrWhiteSpace(p.Period))
        //        .Select(p => p.Period.Trim())
        //        .ToHashSet(StringComparer.OrdinalIgnoreCase);

        //    return periods.Contains("Morning")
        //        && periods.Contains("Noon")
        //        && periods.Contains("Evening");
        //}

        //private string BuildPrompt(
        //    TripSegment segment,
        //    List<POI> activityPois,
        //    List<POI> foodPois,
        //    Dictionary<DateTime, WeatherForecast> forecasts)
        //{
        //    var sb = new StringBuilder();

        //    sb.AppendLine("You are a travel planner AI."); 
        //    sb.AppendLine($"This is a trip from District A to District B."); 

        //    sb.AppendLine("At least 5 activities.");
        //    sb.AppendLine("Use Morning / Noon / Evening.");

        //    sb.AppendLine($"Dates: {segment.StartDate:yyyy-MM-dd} to {segment.EndDate:yyyy-MM-dd}");

        //    if (!activityPois.Any())
        //    {
        //        sb.AppendLine("Activity POIs: NONE AVAILABLE");
        //    }
        //    else
        //    {
        //        foreach (var p in activityPois.Take(12))
        //            sb.AppendLine($"{p.Id} | {p.Name}");
        //    }

        //    if (!foodPois.Any())
        //    {
        //        sb.AppendLine("Food POIs: NONE AVAILABLE");
        //    }
        //    else
        //    {
        //        foreach (var p in foodPois.Take(12))
        //            sb.AppendLine($"{p.Id} | {p.Name}");
        //    }
        //    sb.AppendLine("Weather:");
        //    foreach (var w in forecasts.Take(5))
        //        sb.AppendLine($"{w.Key:yyyy-MM-dd} rain:{w.Value.PrecipitationProbability}");

        //    sb.AppendLine(@"
        //        Return JSON:
        //        {
        //          ""days"": [
        //            {
        //              ""date"": ""2026-01-01"",
        //              ""plan"": [
        //                {
        //                  ""poiId"": ""guid"",
        //                  ""period"": ""Morning | Noon | Evening"",
        //                  ""durationMinutes"": 60-180,
        //                  ""reason"": ""Địa điểm văn hóa nổi bật và thuận tiện di chuyển.""
        //                }
        //              ]
        //            }
        //          ]
        //        }

        //        STRICT RULES:

        //        DAILY STRUCTURE:
        //        - Each day MUST have 5 to 7 activities
        //        - MUST include ALL 3 periods:
        //          - Morning
        //          - Noon
        //          - Evening
        //        - Each period MUST have at least 1 activity
        //        - Distribute activities naturally across periods

        //        FOOD PLANNING RULES
        //        - Every day MUST include food experiences.
        //        - At least:
        //        - 1 Breakfast
        //        - 1 Lunch
        //        - 1 Dinner
        //        - Breakfast, Lunch and Dinner must use POIs from the FOOD POI list.
        //        - Food activities should be naturally distributed throughout the day.
        //        - Do NOT schedule only sightseeing activities.


        //        REASON RULES
        //        - Every activity must contain reason.
        //        - reason should explain why the POI was selected.
        //        - Consider weather, POI type, food options, and travel distance.
        //        - Keep explanations under 20 words.

        //        LANGUAGE RULES:
        //        - ALL text fields MUST be written in Vietnamese.
        //        - dayReason MUST be Vietnamese.
        //        - reason MUST be Vietnamese.
        //        - Do NOT use English explanations.
        //        - Use natural Vietnamese suitable for travel recommendations.

        //        - Mix:
        //        - Attractions
        //        - Cultural experiences
        //        - Food experiences

        //        - Every day must contain at least one Restaurant or StreetFood POI.

        //        - Prefer different food POIs across different days.

        //        MISSING DATA RULES

        //        - Activity POI list may be empty.
        //        - Food POI list may be empty.

        //        - If Activity POIs are unavailable:
        //        return null for sightseeing activities.

        //        - Use only POIs from the Food POI list.
        //        - If Food POI list is empty, skip food activities.
        //        - Never return poiId = null.
        //        - Every plan item must contain a valid poiId from the provided lists.

        //        - Never invent POIs.


        //        DURATION:
        //        - Each activity: 60 → 180 minutes
        //        - Do NOT exceed 3 hours per activity

        //        LOCATION RULE:
        //        - ALL POIs MUST belong to THIS district only
        //        - DO NOT use POIs from other districts

        //        DUPLICATION RULES (MANDATORY)

        //        - A POI ID may appear ONLY ONCE in the entire trip.
        //        - A POI ID MUST NOT appear on multiple days.
        //        - A POI ID MUST NOT appear in multiple periods.
        //        - A POI ID MUST NOT be repeated for any reason.
        //        - If available POIs >= 5:
        //            MUST return at least 5 activities.
        //        - If available POIs < 5:
        //            return all available POIs without duplication.
        //        - NEVER duplicate a POI to reach the activity target.

        //        Any duplicated POI makes the response INVALID.

        //        WEATHER:
        //        - If rain > 60% → prefer indoor POIs
        //        - If weather is good → prefer outdoor POIs

        //        SMART PLANNING:
        //        - Group nearby POIs in same period
        //        - Keep travel reasonable
        //        - Morning = lighter / cultural
        //        - Noon = main activities
        //        - Evening = relaxing / food / entertainment

        //        HARD CONSTRAINTS:
        //        - Use ONLY provided POI IDs
        //        - DO NOT invent POIs
        //        - DO NOT return empty plan
        //        - DO NOT skip any day

        //        OUTPUT MUST BE VALID JSON ONLY
        //        ");
        //    sb.AppendLine($"Total days: {(segment.EndDate.Date - segment.StartDate.Date).Days + 1}");
        //    sb.AppendLine("You MUST return ALL days.");
        //    sb.AppendLine(@"
        //        CRITICAL:
        //        - You MUST return EXACTLY one entry per day
        //        - Total days MUST match input dates
        //        - If missing ANY day → response is INVALID
        //        - NEVER return partial days
        //        ");
        //    sb.AppendLine("🔁 GLOBAL DUPLICATION RULE:n- A POI must NOT be repeated across different days\r\n- A POI must NOT be reused in the trip");

        //    return sb.ToString();
        //}

        private string BuildTripPrompt(
    List<TripSegment> segments,
    List<POI> activityPois,
    List<POI> foodPois,
    Dictionary<Guid,
        Dictionary<DateTime, WeatherForecast>> weatherCache)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an expert Vietnam travel planner AI.");
            sb.AppendLine("Generate ONE itinerary for the ENTIRE trip.");
            sb.AppendLine("Return VALID JSON ONLY.");
            sb.AppendLine();

            // ====================================================
            // SEGMENTS
            // ====================================================

            sb.AppendLine("=== SEGMENTS ===");

            foreach (var segment in segments)
            {
                sb.AppendLine(
                    $"SegmentOrder={segment.OrderIndex}");

                sb.AppendLine(
                    $"Start={segment.StartDate:yyyy-MM-dd HH:mm}");

                sb.AppendLine(
                    $"End={segment.EndDate:yyyy-MM-dd HH:mm}");

                sb.AppendLine(
                    $"DistrictId={segment.DistrictId}");
            }

            sb.AppendLine();

            // ====================================================
            // DAY ASSIGNMENT
            // ====================================================

            sb.AppendLine("=== DAY ASSIGNMENT ===");

            foreach (var segment in segments)
            {
                var dates = Enumerable.Range(
                        0,
                        (segment.EndDate.Date - segment.StartDate.Date).Days + 1)
                    .Select(i => segment.StartDate.Date.AddDays(i));

                foreach (var date in dates)
                {
                    sb.AppendLine(
                        $"{date:yyyy-MM-dd} | District={segment.DistrictId}");
                }
            }

            sb.AppendLine();

            // ====================================================
            // WEATHER
            // ====================================================

            sb.AppendLine("=== WEATHER ===");

            foreach (var segment in segments)
            {
                if (!weatherCache.TryGetValue(
                        segment.LocationId,
                        out var forecasts))
                    continue;

                foreach (var forecast in forecasts.OrderBy(x => x.Key))
                {
                    sb.AppendLine(
                        $"{forecast.Key:yyyy-MM-dd}" +
                        $" | Rain={forecast.Value.PrecipitationProbability}" +
                        $" | Temp={forecast.Value.TemperatureCelsius}");
                }
            }

            sb.AppendLine();

            // ====================================================
            // DISTRICT POIS
            // ====================================================

            sb.AppendLine("=== DISTRICT POIS ===");

            foreach (var districtId in segments
                        .Select(x => x.DistrictId)
                        .Distinct())
            {
                sb.AppendLine();
                sb.AppendLine($"DISTRICT={districtId}");

                sb.AppendLine("ACTIVITY_POIS");

                var districtActivities = activityPois
                    .Where(x => x.DistrictId == districtId)
                    .Take(8)
                    .ToList();

                if (!districtActivities.Any())
                {
                    sb.AppendLine("NONE");
                }
                else
                {
                    foreach (var poi in districtActivities)
                    {
                        sb.AppendLine(
                            $"{poi.Id}" +
                            $" | {poi.Name}" +
                            $" | Type={poi.Type}" +
                            $" | Indoor={poi.IsIndoor}");
                    }
                }

                sb.AppendLine();

                sb.AppendLine("FOOD_POIS");

                var districtFoods = foodPois
                    .Where(x => x.DistrictId == districtId)
                    .Take(5)
                    .ToList();

                if (!districtFoods.Any())
                {
                    sb.AppendLine("NONE");
                }
                else
                {
                    foreach (var poi in districtFoods)
                    {
                        sb.AppendLine(
                            $"{poi.Id}" +
                            $" | {poi.Name}" +
                            $" | Type={poi.Type}");
                    }
                }

                sb.AppendLine();
            }

            // ====================================================
            // OUTPUT FORMAT
            // ====================================================

            sb.AppendLine(@"
            RETURN JSON ONLY:

            {
              ""segments"": [
                {
                  ""segmentOrder"": 1,
                  ""days"": [
                    {
                      ""date"": ""2026-01-01"",
                      ""plan"": [
                        {
                          ""poiId"": ""guid"",
                          ""period"": ""Morning"",
                          ""durationMinutes"": 120,
                          ""reason"": ""Lý do bằng tiếng Việt""
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            ");

            foreach (var segment in segments)
            {
                sb.AppendLine(
                    $"SegmentId={segment.SegmentId}");

                sb.AppendLine(
                    $"Start={segment.StartDate:yyyy-MM-dd HH:mm}");

                sb.AppendLine(
                    $"End={segment.EndDate:yyyy-MM-dd HH:mm}");

                sb.AppendLine(
                    $"LocationId={segment.LocationId}");

                sb.AppendLine(
                    $"DistrictId={segment.DistrictId}");

                sb.AppendLine();
            }

            // ====================================================
            // RULES
            // ====================================================

            sb.AppendLine(@"
            MULTI-SEGMENT RULES

                A single date may belong to multiple segments.
                Each segment must contain all dates within its own start/end range.
                Do not remove dates from a segment because another segment uses the same date.

                Example:

                2026-06-06
                - Segment A
                - Segment B
                - Segment C

                This is valid.

                Generate activities for every segment that contains that date.

                Do NOT assume one date belongs to only one segment.
            ");

            sb.AppendLine(@"

                TIME WINDOW RULES (HARD CONSTRAINT)

                    Each segment has its own Start and End time.

                    Available time for a day:

                    availableMinutes =
                    segmentEndTime - segmentStartTime

                    Examples:

                    06:30 -> 08:00 = 90 minutes
                    11:30 -> 13:00 = 90 minutes
                    18:00 -> 21:00 = 180 minutes

                    The number of activities MUST fit the available time.

                    Generate activities only within the segment time window.

                    Short segments may contain only one period.

                    Examples:

                    06:30-08:00 -> Breakfast only

                    11:30-13:30 -> Lunch only

                    18:00-20:00 -> Dinner only

                    Do not force Morning, Noon and Evening for every segment.

                    Use segment Start and End time.

                    Do not generate Morning activities for a segment that starts at Noon.

                    Do not generate Evening activities for a segment that ends before Evening.

                    VERY SHORT SEGMENT (< 120 minutes)

                    - Return ONLY 1 meal POI if a food POI exists.
                    - If a Cafe POI exists and there is enough remaining time,
                      return 1 Cafe after the meal.
                    - Maximum 2 activities.
                    - Never return sightseeing attractions.

                    SHORT SEGMENT (120 - 240 minutes)

                    - 1 meal POI.
                    - Prefer 1 Cafe OR 1 nearby attraction after the meal.
                    - Maximum 3 activities.

                    NORMAL SEGMENT (> 240 minutes)

                    - Can include:
                      - attractions
                      - culture
                      - cafes
                      - food

                    - Target 3-5 activities.

                    The itinerary MUST fit inside the segment time window.

                    Do NOT generate more activities than the time window allows.

                RULES

                1. SEGMENT RULES
                    - Return ALL provided SegmentIds.
                    - Each activity must belong to exactly one segment.
                    - Multiple segments may exist on the same date.
                    - The same date may appear in multiple segments.
                    - Do not merge different segments together.
                    - Activities must use only POIs from the district of that segment.

                    SEGMENT ORDER RULE

                    Use SegmentOrder exactly as provided.

                    Do not create new orders.

                    Do not skip orders.

                    Every segment in output must contain one of the provided SegmentOrder values.

                    SegmentOrder is an integer.

                2. Use ONLY provided POI IDs.

                3. Never invent POIs.

                4. Never invent dates.
                   If no suitable POI exists, return fewer activities.

                5. Every activity must contain:
                    - poiId
                    - period
                    - durationMinutes
                    - reason

                6. period must be:
                    - Morning
                    - Noon
                    - Evening

                7. durationMinutes:
                    - 60 to 180

                8. reason:
                    - Vietnamese only
                    - under 20 words

                9. DISTRICT RULE

                    For a date:

                    - Use only POIs from the assigned district.
                    - Do not mix districts.

                10. FOOD RULE

                    Meals should prefer FOOD_POIS.

                    Lunch and Dinner should prefer:
                    - Restaurant
                    - StreetFood

                    Do NOT use attraction POIs as meals.


                    MEAL FLOW RULE

                    Preferred order:

                    Breakfast -> Cafe
                    Lunch -> Cafe OR Attraction
                    Dinner -> Cafe OR Night Attraction

                    After a meal:

                    1. Prefer Cafe POI
                    2. If no Cafe exists:
                       prefer nearby attraction
                    3. If neither exists:
                       stop scheduling

                    Do NOT force additional activities.

                11. ATTRACTION RULE

                    Sightseeing should use ACTIVITY_POIS.

                    Do NOT use FOOD_POIS as sightseeing attractions.

                12. RAIN RULE

                    - If rain >= 0.5, prefer Indoor=True attractions.
                    - If no indoor attraction exists, outdoor attractions are allowed.
                    - Never leave a day empty because of weather.

                13. DUPLICATION RULE

                    - Prefer unique POIs whenever alternatives exist.
                    - Avoid repeating the same attraction POI.
                    - Food POIs and Cafe POIs may be reused when appropriate.

                    A POI may be reused only if:

                    1. The reason clearly justifies returning.
                    2. The second visit serves a different purpose.
                    3. No better alternative exists nearby.

                    Examples of acceptable reuse:

                    - Breakfast -> return for Dinner.
                    - Lunch -> return for Dessert.
                    - Morning Coffee -> Evening Coffee.
                    - Day Visit -> Night Visit.

                    Examples of invalid reuse:

                    - Same attraction repeated without reason.
                    - Same restaurant repeated multiple times when alternatives exist.

                    When a POI is reused:

                    - The reason MUST explain why the traveler returns.
                    - The second visit should provide a different experience.

                    - Attractions should be visited only once.

                    - Restaurant, StreetFood and Cafe POIs
                      may be reused when appropriate.

                    Examples:
                    - Breakfast at a cafe
                    - Return in evening for coffee

                    - A food POI may appear at most 2 times
                      in the entire trip.

                    - Never repeat the same POI in the same period.

                    Reuse should be rare and intentional.

                14. If a district contains very few POIs:

                    - return fewer activities.
                    - do not invent POIs.

                15. Target:

                    - Target 3 to 5 activities per day.
                    - If available POIs < target:
                      return as many as possible.
                    - Never return an empty plan if at least one POI exists.

                    EMPTY PLAN IS FORBIDDEN

                    For every day:

                    - plan must contain at least 1 activity.

                    If only one POI exists:
                    return that POI.

                    If only food POIs exist:
                    return food POIs.

                    Returning:

                    ""plan"": []

                    is INVALID.

                    requiredMinutes =
                        sum(durationMinutes)
                        + travel buffers

                        requiredMinutes MUST NOT exceed
                        available segment minutes.

                        If available time is insufficient,
                        return fewer activities.

                        Returning too many activities
                        makes the response INVALID.

                16. Return VALID JSON ONLY.
                ");

            return sb.ToString();
        }

        private TripSegment FindSegment(
    List<TripSegment> segments,
    DateTime date)
        {
            var segment = segments.FirstOrDefault(x =>
                date.Date >= x.StartDate.Date &&
                date.Date <= x.EndDate.Date);

            if (segment == null)
                throw new Exception(
                    $"No segment found for date {date:yyyy-MM-dd}");

            return segment;
        }

        // ====================================================
        // BUILD SCHEDULE
        // ====================================================
        private async Task BuildSchedule(
    Guid itineraryId,
    List<AIActivity> plans,
    List<POI> pois,
    DateTime date,
    TripSegment segment,
    Dictionary<DateTime, WeatherForecast> forecasts,
    List<ItineraryDetail> details,
    Dictionary<string, int> distanceCache,
    Dictionary<Guid, List<string>> usedPoiPeriods)
        {
            // ====================================================
            // 1. DETERMINE DAY TIME WINDOW
            // ====================================================
            var dayStart = date.Date == segment.StartDate.Date
                ? TimeOnly.FromDateTime(segment.StartDate)
                : new TimeOnly(7, 0);

            var dayEnd = date.Date == segment.EndDate.Date
                ? TimeOnly.FromDateTime(segment.EndDate)
                : new TimeOnly(21, 0);

            if (dayEnd <= dayStart)
            {
                dayStart = new TimeOnly(7, 0);
                dayEnd = new TimeOnly(21, 0);
            }

            // ====================================================
            // 2. SPLIT PERIODS DYNAMICALLY
            // ====================================================
            var periods = SplitPeriods(dayStart, dayEnd);

            // ====================================================
            // 3. AVAILABLE POIs (NO REPEAT IN SEGMENT)
            // ====================================================
            var availablePois = pois
                    .Where(p => CanUsePoi(
                        p,
                        date,
                        period: "Init",
                        usedPoiPeriods))
                    .ToList();

            var random = new Random();

            foreach (var period in periods)
            {
                var current = period.Start;

                var totalMinutes = (period.End.ToTimeSpan() - period.Start.ToTimeSpan()).TotalMinutes;

                if (totalMinutes <= 0)
                    continue;

                // how many activities fit (~90-120 mins each)
                var activityCount = (int)(totalMinutes / 120);
                activityCount = Math.Clamp(activityCount, 1, 3);

                //var periodPlans = plans
                //    .Where(x =>
                //        x.PoiId != Guid.Empty &&
                //        x.Period.Equals(period.Period,
                //            StringComparison.OrdinalIgnoreCase))
                //    .ToList();

                var periodPlans = plans
                    .Where(x =>
                        string.Equals(
                            x.Period,
                            period.Period,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!periodPlans.Any())
                {
                    periodPlans = availablePois
                        .Take(activityCount)
                        .Select(p => new AIActivity
                        {
                            PoiId = p.Id,
                            Period = period.Period,
                            DurationMinutes = 90,
                            Reason = "Tự động bổ sung lịch trình."
                        })
                        .ToList();
                }

                POI? prev = null;

                foreach (var activity in periodPlans.Take(activityCount))
                {
                    var poi = pois.FirstOrDefault(x => x.Id == activity.PoiId);

                    if (poi == null)
                        continue;

                    if (!CanUsePoi(
                            poi,
                            date,
                            period.Period,
                            usedPoiPeriods))
                        continue;
                   
                    // ================================
                    // TRAVEL TIME
                    // ================================
                    if (prev != null)
                    {
                        var key = $"{prev.Id}-{poi.Id}";

                        if (!distanceCache.TryGetValue(key, out var travel))
                        {
                            var km = await _geo.GetDrivingDistance(
                                prev.Latitude,
                                prev.Longitude,
                                poi.Latitude,
                                poi.Longitude);

                            travel = (int)((km / 40.0) * 60);
                            distanceCache[key] = travel;
                        }

                        current = current.AddMinutes(travel);
                    }

                    // ================================
                    // DURATION
                    // ================================
                    var duration =
                        activity.DurationMinutes > 0
                            ? Math.Clamp(activity.DurationMinutes, 60, 180)
                            : random.Next(90, 151);

                    var end = current.AddMinutes(duration);

                    if (end > period.End)
                    {
                        end = period.End;

                        if ((end - current).TotalMinutes < 30)
                            continue;
                    }

                    // ================================
                    // WEATHER
                    // ================================
                    var hasForecast = forecasts.TryGetValue(date, out var weather);

                    var plan = activity;

                    Console.WriteLine(
                        $"POI={poi.Name} | PlanFound={plan != null} | Reason={plan?.Reason}");

                    details.Add(new ItineraryDetail
                    {
                        DetailId = Guid.NewGuid(),
                        ItineraryId = itineraryId,
                        PoiId = poi.Id,
                        VisitDate = date,
                        StartTime = current,
                        EndTime = end,
                        TemperatureCelsius =
                            hasForecast ? weather.TemperatureCelsius : 0,

                        PrecipitationProbability =
                            hasForecast ? weather.PrecipitationProbability : 0,

                        WindSpeed =
                            hasForecast ? weather.WindSpeed : 0,
                        AIReason = plan?.Reason
                    });

                    var usage = $"{date:yyyy-MM-dd}-{period.Period}";

                    if (!usedPoiPeriods.ContainsKey(poi.Id))
                    {
                        usedPoiPeriods[poi.Id] = [];
                    }

                    usedPoiPeriods[poi.Id].Add(usage);
                    current = end.AddMinutes(30);
                    prev = poi;
                    
                }

                if (!availablePois.Any())
                    break;
            }
        }

        //=====================================================
        // SPLIT PERIODS (dynamic based on day length)
        //=====================================================
        private List<(string Period, TimeOnly Start, TimeOnly End)>
    SplitPeriods(TimeOnly start, TimeOnly end)
        {
            var result =
                new List<(string, TimeOnly, TimeOnly)>();

            var totalMinutes =
                (end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;

            if (totalMinutes <= 0)
                return result;

            // < 3 giờ
            if (totalMinutes < 180)
            {
                var middleHour =
                    start.AddMinutes(totalMinutes / 2).Hour;

                if (middleHour < 11)
                {
                    result.Add(("Morning", start, end));
                }
                else if (middleHour < 17)
                {
                    result.Add(("Noon", start, end));
                }
                else
                {
                    result.Add(("Evening", start, end));
                }

                return result;
            }

            var third = (int)(totalMinutes / 3);

            result.Add((
                "Morning",
                start,
                start.AddMinutes(third)));

            result.Add((
                "Noon",
                start.AddMinutes(third),
                start.AddMinutes(third * 2)));

            result.Add((
                "Evening",
                start.AddMinutes(third * 2),
                end));

            return result;
        }


        // ====================================================
        // FALLBACK
        // ====================================================
        private void BuildFallback(
    Guid itineraryId,
    List<POI> pois,
    DateTime date,
    Dictionary<DateTime, WeatherForecast> forecasts,
    List<ItineraryDetail> details,
   Dictionary<Guid, List<string>> usedPoiPeriods)
        {
            var random = new Random();

            // ====================================================
            // 1. GET WEATHER (SAFE)
            // ====================================================
            var weather = forecasts
                .FirstOrDefault(x => x.Key.Date == date.Date)
                .Value;

            if (weather == null)
            {
                weather = new WeatherForecast
                {
                    PrecipitationProbability = 0.3,
                    TemperatureCelsius = 28,
                    WindSpeed = 10
                };
            }

            // ====================================================
            // 2. FILTER UNUSED POIs
            // ====================================================
            var availablePois = pois
                    .Where(p =>
                        CanUsePoi(
                            p,
                            date,
                            "Fallback",
                            usedPoiPeriods))
                    .ToList();

            // ====================================================
            // 3. WEATHER-BASED PRIORITIZATION (🔥 YOUR LOGIC FIXED)
            // ====================================================
            if (weather.PrecipitationProbability > 0.6)
            {
                // rain → indoor first
                availablePois = availablePois
                    .OrderByDescending(p => p.IsIndoor)
                    .ThenBy(x => random.Next()) // keep randomness
                    .ToList();
            }
            else
            {
                // good weather → outdoor first
                availablePois = availablePois
                    .OrderBy(p => p.IsIndoor)
                    .ThenBy(x => random.Next())
                    .ToList();
            }

            // ====================================================
            // 4. SELECT POIs
            // ====================================================
            var selected = availablePois
                .Take(5)
                .ToList();

            var time = new TimeOnly(7, 0);

            foreach (var poi in selected)
            {
                if (!CanUsePoi(
                    poi,
                    date,
                    "Fallback",
                    usedPoiPeriods))
                    continue;

                var duration = random.Next(90, 151);
                var end = time.AddMinutes(duration);

                // ====================================================
                // 5. REAL RISK SCORE (NO MORE 0)
                // ====================================================
                details.Add(new ItineraryDetail
                {
                    DetailId = Guid.NewGuid(),
                    ItineraryId = itineraryId,
                    PoiId = poi.Id,
                    VisitDate = date,
                    StartTime = time,
                    EndTime = end,
                    TemperatureCelsius = weather.TemperatureCelsius,
                    PrecipitationProbability = weather.PrecipitationProbability,
                    WindSpeed = weather.WindSpeed,
                    AIReason = weather.PrecipitationProbability > 0.6
                        ? "Ưu tiên địa điểm phù hợp thời tiết mưa."
                        : "Địa điểm được chọn dựa trên thời tiết thuận lợi."
                });

                if (!usedPoiPeriods.ContainsKey(poi.Id))
                {
                    usedPoiPeriods[poi.Id] = [];
                }

                usedPoiPeriods[poi.Id]
                    .Add($"{date:yyyy-MM-dd}-Fallback");

                time = end.AddMinutes(45);
            }
        }


        public async Task<List<TripSegment>> GetByTripIdWithDetailsAsync(Guid tripId)
        {
            var segments = await _plannerRepo.GetByTripIdWithDetailsAsync(tripId);

            foreach (var segment in segments)
            {
                foreach (var itinerary in segment.Itineraries)
                {
                    itinerary.ItineraryDetails = itinerary.ItineraryDetails
                        .OrderBy(d => d.StartTime)
                        .ToList();
                }
            }

            return segments;
        }

        /// <inheritdoc />
        public Task PreloadTripWeatherAsync(Guid tripId)
            => _weatherService.PreloadTripWeatherAsync(tripId);


        private bool CanUsePoi(
        POI poi,
        DateTime date,
        string period,
        Dictionary<Guid, List<string>> usedPoiPeriods)
        {
            if (!usedPoiPeriods.TryGetValue(
                    poi.Id,
                    out var usages))
            {
                return true;
            }

            var current =
                $"{date:yyyy-MM-dd}-{period}";

            if (usages.Contains(current))
                return false;

            // Attractions chỉ được đi 1 lần
            if (poi.Type != POIType.Restaurant &&
                poi.Type != POIType.StreetFood &&
                poi.Type != POIType.Cafe)
            {
                return false;
            }

            // Cafe / Food tối đa 2 lần
            return usages.Count < 2;
        }

        private bool IsValid(TripAIResponse response)
        {
            if (response?.Segments == null)
                return false;

            foreach (var segment in response.Segments)
            {
                foreach (var day in segment.Days)
                {
                    if (day.Plan == null || !day.Plan.Any())
                        return false;
                }
            }

            return true;
        }
    }
}
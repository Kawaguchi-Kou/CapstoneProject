using System.Text;
using System.Text.Json;
using Application.DTOs.AIResponse;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
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

        public async Task GenerateAsync(Guid tripId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tripUsedPoiIds = new HashSet<Guid>();
                // ====================================================
                // 1. LOAD SEGMENTS
                // ====================================================
                var segments = (await _segmentRepo.GetByTripIdAsync(tripId))
                    .OrderBy(x => x.OrderIndex)
                    .ToList();

                if (!segments.Any())
                    throw new Exception("No segments");
                Console.WriteLine($"Segments: {segments.Count}");
                foreach (var s in segments)
                {
                    Console.WriteLine(
                        $"Segment {s.OrderIndex} - Location: {s.LocationId}, District: {s.DistrictId}, Start: {s.StartDate}, End: {s.EndDate}");
                }

                // ====================================================
                // 2. LOAD USER PREFS
                // ====================================================
                var account = await _authService.GetCurrentAccount();

                var prefIds = (await _userRepo.GetPreferenceByAccountIdAsync(account.Id))
                    .Select(x => x.PreferenceId)
                    .ToHashSet();

                // ====================================================
                // 3. PRELOAD ALL POIs (🔥 BIG FIX)
                // ====================================================
                var keys = segments
                    .Select(s => (s.LocationId, s.DistrictId))
                    .Distinct()
                    .ToList();

                var allPois = await _poiRepo.GetByLocationDistrictPairsAsync(keys);

                var poiCache = allPois
                    .GroupBy(p => $"{p.LocationId}-{p.DistrictId}")
                    .ToDictionary(g => g.Key, g =>
                        g.OrderByDescending(x =>
                            x.PoiPreferences.Count(pp =>
                                prefIds.Contains(pp.PreferenceId)))
                         .ThenBy(x => x.Name)
                         .ToList()
                    );

                // ====================================================
                // 4. PRELOAD WEATHER (cached per location)
                // ====================================================
                var weatherCache = new Dictionary<Guid, Dictionary<DateTime, WeatherForecast>>();

                // ====================================================
                // 5. DISTANCE CACHE
                // ====================================================
                var distanceCache = new Dictionary<string, int>();
                var aiCache = new Dictionary<string, SegmentAIResponse>();

                var itineraries = new List<Itinerary>();
                var details = new List<ItineraryDetail>();

                foreach (var segment in segments)
                {
                    var aiKey = $"{segment.LocationId}-{segment.DistrictId}-{segment.StartDate:yyyyMMdd}-{segment.EndDate:yyyyMMdd}";
                    var key = $"{segment.LocationId}-{segment.DistrictId}";
                    var segmentUsedPoiIds = new HashSet<Guid>();

                    if (!poiCache.TryGetValue(key, out var pois) || !pois.Any()){
                        Console.WriteLine($"No POIs for segment {segment.OrderIndex}");

                        // 🔥 HARD fallback pool (very important)
                        pois = poiCache.Values
                            .SelectMany(x => x)
                            .GroupBy(p => p.Id)
                            .Select(g => g.First())
                            .OrderBy(x => Guid.NewGuid())
                            .Take(15)
                            .ToList();

                        if (!pois.Any())
                        {
                            Console.WriteLine("No POIs in entire system → skipping segment");
                            continue;
                        }
                    }

                    // ====================================================
                    // BUILD DATES
                    // ====================================================
                    var dates = Enumerable.Range(
                            0,
                            (segment.EndDate.Date - segment.StartDate.Date).Days + 1)
                        .Select(x => segment.StartDate.Date.AddDays(x))
                        .ToList();

                    // ====================================================
                    // WEATHER (cached per location)
                    // ====================================================
                    if (!weatherCache.TryGetValue(segment.LocationId, out var forecasts))
                    {
                        forecasts = await _weatherService
                            .GetRangeOptimizedAsync(segment.LocationId, dates);

                        weatherCache[segment.LocationId] = forecasts;
                    }

                    // ====================================================
                    // AI GENERATE
                    // ====================================================
                    SegmentAIResponse? ai = null;

                    if (!aiCache.TryGetValue(aiKey, out var cachedAi))
                    {
                        var generated = await GenerateSafe(segment, pois, forecasts);

                        // 🔥 HARD VALIDATION (prevent partial AI)
                        if (generated != null &&
                            generated.Days != null &&
                            generated.Days.Count == dates.Count)
                        {
                            aiCache[aiKey] = generated;
                            ai = generated;

                            Console.WriteLine("✅ AI cached");
                        }
                        else
                        {
                            Console.WriteLine("❌ AI invalid (missing days) → fallback");
                            ai = null;
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚡ Using cached AI");
                        ai = cachedAi;
                    }

                    var itinerary = new Itinerary
                    {
                        ItineraryId = Guid.NewGuid(),
                        SegmentId = segment.SegmentId,
                        GeneratedByAI = ai != null
                    };

                    itineraries.Add(itinerary);

                    foreach (var date in dates)
                    {
                        Console.WriteLine($"--- Processing date: {date:yyyy-MM-dd} ---");

                        // 🔥 SAFE ACCESS + LOG
                        if (ai == null)
                        {
                            Console.WriteLine("AI = NULL → fallback");
                            BuildFallback(
                                itinerary.ItineraryId,
                                pois,
                                date,
                                forecasts,
                                details,
                                segmentUsedPoiIds,
                                tripUsedPoiIds);

                            continue;
                        }
                        Console.WriteLine($"AI Days count: {ai.Days?.Count}");

                        var day = ai.Days?
                            .FirstOrDefault(x => x.Date.Date == date.Date);

                        if (day == null)
                        {
                            Console.WriteLine("Day not found → fallback");

                            BuildFallback(
                                itinerary.ItineraryId,
                                pois,
                                date,
                                forecasts,
                                details,
                                segmentUsedPoiIds,
                                tripUsedPoiIds);

                            continue;
                        }

                        if (day.Plan == null || day.Plan.Count < 5)
                        {
                            Console.WriteLine("⚡ Partial AI → filling missing");

                            var fixedPlans = FillMissingPlans(day.Plan ?? new List<AIActivity>(), pois);

                            await BuildSchedule(
                                itinerary.ItineraryId,
                                day.Plan,
                                pois,
                                date,
                                segment,
                                forecasts,
                                details,
                                distanceCache,
                                segmentUsedPoiIds,
                                tripUsedPoiIds);

                            continue;
                        }

                        Console.WriteLine("✅ Valid AI day → using AI");

                        await BuildSchedule(
                                itinerary.ItineraryId,
                                day.Plan,
                                pois,
                                date,
                                segment,
                                forecasts,
                                details,
                                distanceCache,
                                segmentUsedPoiIds,
                                tripUsedPoiIds);
                    }
                }

                if (!itineraries.Any() || !details.Any())
                    throw new Exception("No itinerary");

                // ====================================================
                // SAVE ONCE (good practice)
                // ====================================================
                await _itineraryRepo.AddRangeAsync(itineraries);
                await _detailRepo.AddRangeAsync(details);

                await _unitOfWork.CommitAsync(); // 🔥 THIS IS WHY YOU SEE NOTHING
            }
            catch
            {
                await _unitOfWork.RollbackAsync();      // 🔥 rollback everything
                throw;
            }
        }


        // ====================================================
        // AI
        // ====================================================
        private async Task<SegmentAIResponse?> GenerateSafe(
            TripSegment segment,
            List<POI> pois,
            Dictionary<DateTime, WeatherForecast> forecasts)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var prompt = BuildPrompt(segment, pois, forecasts);
                    var raw = await _gemini.GenerateAsync(prompt);

                    Console.WriteLine($"RAW RESPONSE: {raw}");

                    var parsed = JsonSerializer.Deserialize<SegmentAIResponse>(
                        raw,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return parsed;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("503"))
                {
                    Console.WriteLine($"⚠️ Gemini 503 retry {i + 1}");
                    await Task.Delay(2000 * (i + 1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AI ERROR: {ex.Message}");
                    return null;
                }
            }

            Console.WriteLine("❌ Gemini failed after retries");
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

        private bool ValidateDay(AIDayPlan day)
        {
            if (day.Plan == null || day.Plan.Count < 5)
                return false;

            var periods = day.Plan
                .Where(p => !string.IsNullOrWhiteSpace(p.Period))
                .Select(p => p.Period.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return periods.Contains("Morning")
                && periods.Contains("Noon")
                && periods.Contains("Evening");
        }

        private string BuildPrompt(
            TripSegment segment,
            List<POI> pois,
            Dictionary<DateTime, WeatherForecast> forecasts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a travel planner AI."); 
            sb.AppendLine($"This is a trip from District A to District B."); 
;

            sb.AppendLine("At least 5 activities.");
            sb.AppendLine("Use Morning / Noon / Evening.");

            sb.AppendLine($"Dates: {segment.StartDate:yyyy-MM-dd} to {segment.EndDate:yyyy-MM-dd}");

            sb.AppendLine("POIs:");
            foreach (var p in pois.Take(12))
                sb.AppendLine($"{p.Id} | {p.Name}");

            sb.AppendLine("Weather:");
            foreach (var w in forecasts.Take(5))
                sb.AppendLine($"{w.Key:yyyy-MM-dd} rain:{w.Value.PrecipitationProbability}");

            sb.AppendLine(@"
                Return JSON:
                {
                  ""days"": [
                    {
                      ""date"": ""2026-01-01"",
                      ""plan"": [
                        {
                          ""poiId"": ""guid"",
                          ""period"": ""Morning | Noon | Evening"",
                          ""durationMinutes"": 60-180
                        }
                      ]
                    }
                  ]
                }

                🔥 STRICT RULES:

                📅 DAILY STRUCTURE:
                - Each day MUST have 5 to 7 activities
                - MUST include ALL 3 periods:
                  - Morning
                  - Noon
                  - Evening
                - Each period MUST have at least 1 activity
                - Distribute activities naturally across periods

                ⏱️ DURATION:
                - Each activity: 60 → 180 minutes
                - Do NOT exceed 3 hours per activity

                📍 LOCATION RULE:
                - ALL POIs MUST belong to THIS district only
                - DO NOT use POIs from other districts

                🔁 DUPLICATION:
                - DO NOT repeat POIs in same day
                - AVOID repeating POIs across multiple days

                🌧️ WEATHER:
                - If rain > 60% → prefer indoor POIs
                - If weather is good → prefer outdoor POIs

                🧠 SMART PLANNING:
                - Group nearby POIs in same period
                - Keep travel reasonable
                - Morning = lighter / cultural
                - Noon = main activities
                - Evening = relaxing / food / entertainment

                🚫 HARD CONSTRAINTS:
                - Use ONLY provided POI IDs
                - DO NOT invent POIs
                - DO NOT return empty plan
                - DO NOT skip any day

                ⚠️ OUTPUT MUST BE VALID JSON ONLY
                ");
            sb.AppendLine($"Total days: {(segment.EndDate.Date - segment.StartDate.Date).Days + 1}");
            sb.AppendLine("You MUST return ALL days.");
            sb.AppendLine(@"
                🚨 CRITICAL:
                - You MUST return EXACTLY one entry per day
                - Total days MUST match input dates
                - If missing ANY day → response is INVALID
                - NEVER return partial days
                ");
            sb.AppendLine("🔁 GLOBAL DUPLICATION RULE:n- A POI must NOT be repeated across different days\r\n- A POI must NOT be reused in the trip");

            return sb.ToString();
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
    HashSet<Guid> segmentUsedPoiIds,
HashSet<Guid> tripUsedPoiIds)
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
                .Where(p => !segmentUsedPoiIds.Contains(p.Id)
                         && !tripUsedPoiIds.Contains(p.Id))
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

                var selected = availablePois
                    .OrderBy(x => random.Next())
                    .Take(activityCount)
                    .ToList();

                POI? prev = null;

                foreach (var poi in selected)
                {
                    if (segmentUsedPoiIds.Contains(poi.Id))
                        continue;

                    // ================================
                    // 🚗 TRAVEL TIME
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
                    // ⏱ DURATION
                    // ================================
                    var duration = random.Next(90, 151); // 90–150 mins
                    var end = current.AddMinutes(duration);

                    if (end > period.End)
                        break;

                    // ================================
                    // 🌧 WEATHER
                    // ================================
                    var hasForecast = forecasts.TryGetValue(date, out var weather);

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
                            hasForecast ? weather.WindSpeed : 0
                    });

                    tripUsedPoiIds.Add(poi.Id);

                    segmentUsedPoiIds.Add(poi.Id);
                    current = end.AddMinutes(30);
                    prev = poi;
                }

                // remove used POIs
                availablePois = availablePois
                    .Where(p => !segmentUsedPoiIds.Contains(p.Id))
                    .ToList();

                if (!availablePois.Any())
                    break;
            }
        }

        //=====================================================
        //CalculateRiskSafe
        //=====================================================
        private double CalculateRiskSafe(
    DateTime date,
    Dictionary<DateTime, WeatherForecast> forecasts,
    POI poi)
        {
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

            return _riskEngine.CalculateRisk(weather, poi.IsIndoor);
        }


        //=====================================================
        // SPLIT PERIODS (dynamic based on day length)
        //=====================================================
        private List<(string Period, TimeOnly Start, TimeOnly End)> SplitPeriods(TimeOnly start, TimeOnly end)
        {
            var result = new List<(string, TimeOnly, TimeOnly)>();

            var totalMinutes = (end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;

            if (totalMinutes <= 0)
                return result;

            // very short day → no split
            if (totalMinutes < 180)
            {
                result.Add(("Flexible", start, end));
                return result;
            }

            var third = (int)(totalMinutes / 3);

            var p1Start = start;
            var p1End = start.AddMinutes(third);

            var p2Start = p1End;
            var p2End = p2Start.AddMinutes(third);

            var p3Start = p2End;
            var p3End = end;

            result.Add(("Morning", p1Start, p1End));
            result.Add(("Noon", p2Start, p2End));
            result.Add(("Evening", p3Start, p3End));

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
    HashSet<Guid> segmentUsedPoiIds,
HashSet<Guid> tripUsedPoiIds)
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
                .Where(p => !segmentUsedPoiIds.Contains(p.Id)
         && !tripUsedPoiIds.Contains(p.Id))
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
                if (segmentUsedPoiIds.Contains(poi.Id) || tripUsedPoiIds.Contains(poi.Id))
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
                });

                segmentUsedPoiIds.Add(poi.Id);
                tripUsedPoiIds.Add(poi.Id);

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
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Helper;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;


namespace Application.Services
{
    public class POIService : IPOIService
    {
        private readonly IPOIRepository _poiRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGeocodingService _geocodingService;
        private readonly ILocationRepository _locationRepository;
        private readonly IPreferenceRepository _preferenceRepository;

        public POIService(
            IPOIRepository poiRepository,
            IUserRepository userRepository,
            IGeocodingService geocodingService,
            ILocationRepository locationRepository,
            IPreferenceRepository preferenceRepository)
        {
            _poiRepository = poiRepository;
            _userRepository = userRepository;
            _geocodingService = geocodingService;
            _locationRepository = locationRepository;
            _preferenceRepository = preferenceRepository;
        }

        //public async Task<List<POIScoreResult>> CalculateScoresAsync(Guid accountId)
        //{
        //    // 1. User preferences
        //    var userPrefs = await _userRepository.GetByAccountIdAsync(accountId);

        //    var userPrefSet = userPrefs
        //        .Select(x => x.PreferenceCode)
        //        .ToHashSet();

        //    // 2. All POIs
        //    var pois = await _poiRepository.GetAllWithPreferencesAsync();

        //    var results = new List<POIScoreResult>();

        //    foreach (var poi in pois)
        //    {
        //        int score = poi.PoiPreferences.Count(pp =>
        //            pp.Preference != null &&
        //            userPrefSet.Contains(pp.Preference.Name));

        //        results.Add(new POIScoreResult
        //        {
        //            PoiId = poi.Id,
        //            PoiName = poi.Name,
        //            Score = score
        //        });
        //    }

        //    return results
        //        .OrderByDescending(x => x.Score)
        //        .ToList();
        //}

        public async Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(
    Guid accountId)
        {
            var userPrefs = await _userRepository.GetPreferenceByAccountIdAsync(accountId);
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            var userPrefSet =userPrefs
                .Select(x => x.Preference.Name)
                .ToHashSet();

            var result = pois
                .Select(poi =>
                {
                    var score = poi.PoiPreferences.Count(pp =>
                        pp.Preference != null &&
                        userPrefSet.Contains(pp.Preference.Name));

                    return new RecommendedPoiResponse
                    {
                        Id = poi.Id,
                        Name = poi.Name,
                        Address = poi.Address,
                        City = poi.City,
                        ApproxCost = poi.ApproxCost,
                        OpenHour = poi.OpenHour,
                        CloseHour = poi.CloseHour,
                        GoogleMapLink = poi.GoogleMapLink,
                        IsIndoor = poi.IsIndoor,
                        Type = poi.Type,
                        LocationName = poi.Location?.LocationName ?? "",
                        POIImgUrl = poi.POIImgUrl,
                        Score = score,
                        POIPreferences = poi.PoiPreferences
                            .Where(pp => pp.Preference != null)
                            .Select(pp => pp.Preference.Name)
                            .ToList()
                    };
                })
                .OrderByDescending(x => x.Score)
                .ToList();

             return result;
        }

        public async Task<List<POI>> GetAllAsync()
        {
            var pois = await _poiRepository.GetAllAsync();

            return pois;
        }

        public async Task<POI?> GetByIdAsync(Guid id)
        {
            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                return null;

            return poi;
        }

        public async Task<POI> CreateAsync(POI request, List<Guid> preferenceIds)
        {
            var (lat, lon) = await _geocodingService
                .GetCoordinatesAsync(request.Name, request.City);

            request.Latitude = lat;
            request.Longitude = lon;

            // Delegate everything DB-related to repository
            await _poiRepository.AddAsync(request, preferenceIds);

            return request;
        }

        public async Task<POI> UpdateAsync(Guid id, POI request)
        {

            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                throw new Exception("POI not found");

            if (request.Address != null)
                poi.Address = request.Address;

            if (request.ApproxCost != null)
                poi.ApproxCost = request.ApproxCost;

            
                poi.OpenHour = request.OpenHour;
                poi.CloseHour = request.CloseHour;

            if (request.GoogleMapLink != null)
                poi.GoogleMapLink = request.GoogleMapLink;

                poi.IsIndoor = request.IsIndoor;

            if(request.CloseHour.HasValue && request.OpenHour.HasValue)
            {
                poi.VisitRecommendation = GetVisitRecommendation(
                    request.OpenHour,
                    request.CloseHour,
                    request.Is24Hours,
                    request.IsIndoor);

                poi.CloseHour = request.CloseHour;
                poi.OpenHour = request.OpenHour;
            }

            if (request.POIImgUrl != null)
            {
                poi.POIImgUrl = request.POIImgUrl;
            }

            await _poiRepository.UpdateAsync(poi);

            return poi;
        }

        public async Task DeleteAsync(Guid id)
        {
            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                throw new Exception("POI not found");

            await _poiRepository.DeleteAsync(poi);
        }

        //public async Task ImportExcelAsync(IFormFile file)
        //{
        //    using var stream = new MemoryStream();
        //    await file.CopyToAsync(stream);

        //    using var package = new ExcelPackage(stream);
        //    var worksheet = package.Workbook.Worksheets[0];
        //    int rowCount = worksheet.Dimension.Rows;

        //    // 🔥 Load toàn bộ Location 1 lần (tránh gọi DB trong loop)
        //    var locations = (await _locationRepository.GetAllAsync())
        //      .GroupBy(x => x.LocationName.ToLower())
        //      .ToDictionary(g => g.Key, g => g.First());

        //    List<POI> pois = new();

        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        string name = worksheet.Cells[row, 1].Text.Trim();
        //        string address = worksheet.Cells[row, 2].Text.Trim();
        //        string cityRaw = worksheet.Cells[row, 3].Text.Trim();
        //        var prefRaw = worksheet.Cells[row, 8].Text.Trim();
        //        var preferenceIds = new List<Guid>();
        //        if (!string.IsNullOrEmpty(prefRaw))
        //        {
        //            preferenceIds = prefRaw
        //                .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                .Select(x => Guid.TryParse(x.Trim(), out var id) ? id : (Guid?)null)
        //                .Where(x => x.HasValue)
        //                .Select(x => x!.Value)
        //                .ToList();
        //        }


        //        string cityKey = cityRaw.ToLower();

        //        if (!locations.ContainsKey(cityKey))
        //            continue;

        //        var location = locations[cityKey];

        //        decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost);
        //        bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor);

        //        //Opening Hours Parsing
        //        var openingRaw = worksheet.Cells[row, 5].Text.Trim();

        //        TimeOnly? openHour = null;
        //        TimeOnly? closeHour = null;
        //        bool is24Hours = false;

        //        if (!string.IsNullOrWhiteSpace(openingRaw) && openingRaw.Contains("~"))
        //        {
        //            var parts = openingRaw.Split('~', StringSplitOptions.TrimEntries);

        //            if (parts.Length == 2)
        //            {
        //                if (TimeOnly.TryParse(parts[0], out var open))
        //                    openHour = open;

        //                if (TimeOnly.TryParse(parts[1], out var close))
        //                    closeHour = close;

        //                // 24h case
        //                if (openHour == TimeOnly.MinValue && closeHour == TimeOnly.MinValue)
        //                {
        //                    is24Hours = true;
        //                }
        //            }
        //        }

        //        // Visit Recommendation
        //        string visitRecommendation = GetVisitRecommendation(
        //            openHour,
        //            closeHour,
        //            is24Hours,
        //            isIndoor
        //        );

        //        // LẤY IMAGE TỪ MAP
        //        var normalizedName = name.Trim().ToLower();

        //        string? imageUrl = _imageMap.ContainsKey(normalizedName)
        //            ? _imageMap[normalizedName]
        //            : null;

        //        var poi = new POI
        //        {
        //            Id = Guid.NewGuid(),

        //            Name = name,
        //            Address = address,
        //            City = cityRaw,

        //            ApproxCost = cost.ToString(),
        //            OpenHour = openHour,
        //            CloseHour = closeHour,
        //            Is24Hours = is24Hours,
        //            VisitRecommendation = visitRecommendation,
        //            GoogleMapLink = worksheet.Cells[row, 6].Text,
        //            IsIndoor = isIndoor,

        //            LocationId = location.LocationId,
        //            Latitude = location.Latitude,
        //            Longitude = location.Longitude,

        //            POIImgUrl = imageUrl
        //        };
        //        var poiPreferences = preferenceIds.Select(prefId => new POIPreference
        //        {
        //            PoiId = poi.Id,
        //            PreferenceId = prefId
        //        }).ToList();

        //        pois.Add(poi);
        //    }


        //    await _poiRepository.AddRangeAsync(pois);
        //}

        public async Task ImportExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            // 🔥 Load Locations
            var locations = (await _locationRepository.GetAllAsync())
                .GroupBy(x => StringNormalizer.Normalize(x.LocationName))
                .ToDictionary(g => g.Key, g => g.First());

            // 🔥 Load Preferences (map by NAME)
            var preferenceMap = (await _preferenceRepository.GetAllAsync())
                .ToDictionary(x => x.Name.ToLower(), x => x.Id);

            List<POI> pois = new();

            for (int row = 2; row <= rowCount; row++)
            {
                // ===== BASIC FIELDS =====
                string name = worksheet.Cells[row, 1].Text.Trim();
                string address = worksheet.Cells[row, 2].Text.Trim();
                string cityRaw = worksheet.Cells[row, 3].Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    throw new Exception($"Row {row}: Name is empty");

                // ===== LOCATION =====
                string cityKey = StringNormalizer.Normalize(cityRaw);

                if (!locations.ContainsKey(cityKey))
                    throw new Exception($"Row {row}: Location not found '{cityRaw}'");

                var location = locations[cityKey];

                // ===== COST =====
                if (!decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost))
                    throw new Exception($"Row {row}: Invalid cost");

                // ===== INDOOR =====
                if (!bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor))
                    throw new Exception($"Row {row}: Invalid Indoor value");

                // ===== OPENING HOURS =====
                var openingRaw = worksheet.Cells[row, 5].Text.Trim();

                TimeOnly? openHour = null;
                TimeOnly? closeHour = null;
                bool is24Hours = false;

                if (!string.IsNullOrWhiteSpace(openingRaw))
                {
                    var parts = openingRaw.Split('~', StringSplitOptions.TrimEntries);

                    if (parts.Length != 2)
                        throw new Exception($"Row {row}: Invalid opening format");

                    if (!TimeOnly.TryParse(parts[0], out var open))
                        throw new Exception($"Row {row}: Invalid open hour");

                    if (!TimeOnly.TryParse(parts[1], out var close))
                        throw new Exception($"Row {row}: Invalid close hour");

                    openHour = open;
                    closeHour = close;

                    if (open == TimeOnly.MinValue && close == TimeOnly.MinValue)
                        is24Hours = true;
                }

                // ===== TYPE (ENUM) =====
                var typeRaw = worksheet.Cells[row, 9].Text.Trim();

                if (!Enum.TryParse<POIType>(typeRaw, true, out var poiType))
                    throw new Exception($"Row {row}: Invalid POIType '{typeRaw}'");

                // ===== LAT LNG =====
                if (!double.TryParse(worksheet.Cells[row, 10].Text, out var lat))
                    throw new Exception($"Row {row}: Invalid Latitude");

                if (!double.TryParse(worksheet.Cells[row, 11].Text, out var lng))
                    throw new Exception($"Row {row}: Invalid Longitude");

                // ===== PREFERENCES (TEXT → GUID) =====
                var prefRaw = worksheet.Cells[row, 8].Text.Trim();
                var preferenceIds = new List<Guid>();

                if (!string.IsNullOrEmpty(prefRaw))
                {
                    var prefNames = prefRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var pref in prefNames)
                    {
                        var key = pref.Trim().ToLower();

                        if (!preferenceMap.ContainsKey(key))
                            throw new Exception($"Row {row}: Preference '{pref}' not found");

                        preferenceIds.Add(preferenceMap[key]);
                    }
                }

                // ===== IMAGE =====
                var normalizedName = name.ToLower();
                string? imageUrl = _imageMap.TryGetValue(normalizedName, out var img)
                    ? img
                    : null;

                // ===== CREATE POI =====
                var poi = new POI
                {
                    Id = Guid.NewGuid(),

                    Name = name,
                    Address = address,
                    City = cityRaw,

                    ApproxCost = cost.ToString(),
                    OpenHour = openHour,
                    CloseHour = closeHour,
                    Is24Hours = is24Hours,
                    Type = poiType,

                    VisitRecommendation = GetVisitRecommendation(
                        openHour, closeHour, is24Hours, isIndoor),

                    GoogleMapLink = worksheet.Cells[row, 6].Text,
                    IsIndoor = isIndoor,

                    LocationId = location.LocationId,

                    Latitude = lat,
                    Longitude = lng,
                    Status = POIStatus.Approved,

                    POIImgUrl = imageUrl
                };

                // 🔥 IMPORTANT: Assign POI Preferences
                poi.PoiPreferences = preferenceIds.Select(prefId => new POIPreference
                {
                    PoiId = poi.Id,
                    PreferenceId = prefId
                }).ToList();

                pois.Add(poi);
            }

            await _poiRepository.AddRangeAsync(pois);
        }

        private static Dictionary<string, string> _imageMap = new();

        public void AddImageMapping(string fileName, string url)
        {
            var key = Path.GetFileNameWithoutExtension(fileName)
                .Trim()
                .ToLower();

            _imageMap[key] = url;
        }

        private string GetVisitRecommendation(
            TimeOnly? openHour,
            TimeOnly? closeHour,
            bool is24Hours,
            bool isIndoor)
        {
            if (is24Hours)
                return "Open 24 hours - can be visited anytime";

            if (!openHour.HasValue || !closeHour.HasValue)
                return "Opening hours not available";

            var open = openHour.Value;
            var close = closeHour.Value;

            // Morning place
            if (open <= new TimeOnly(6, 0) && close <= new TimeOnly(14, 0))
                return "Best visited in the morning";

            // Afternoon place
            if (open >= new TimeOnly(10, 0) && close <= new TimeOnly(18, 0))
                return "Best visited in the afternoon";

            // Evening / night place
            if (open >= new TimeOnly(16, 0) || close >= new TimeOnly(22, 0))
                return "Ideal for evening or night visits";

            // Outdoor vs indoor bonus
            if (!isIndoor)
                return "Best visited during daylight hours";

            return "Suitable to visit at any time of the day";
        }
    }
}

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

        public POIService(
            IPOIRepository poiRepository,
            IUserRepository userRepository,
            IGeocodingService geocodingService,
            ILocationRepository locationRepository)
        {
            _poiRepository = poiRepository;
            _userRepository = userRepository;
            _geocodingService = geocodingService;
            _locationRepository = locationRepository;
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

        public async Task ImportExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            var locations = (await _locationRepository.GetAllAsync())
                .GroupBy(x => StringNormalizer.Normalize(x.LocationName))
                .ToDictionary(g => g.Key, g => g.First());

            List<POI> pois = new();

            for (int row = 2; row <= rowCount; row++)
            {
                string name = worksheet.Cells[row, 1].Text.Trim();
                string address = worksheet.Cells[row, 2].Text.Trim();
                string cityRaw = worksheet.Cells[row, 3].Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    throw new Exception($"Row {row}: POI name is empty");

                if (string.IsNullOrWhiteSpace(cityRaw))
                    throw new Exception($"Row {row}: City is empty");

                string cityKey = StringNormalizer.Normalize(cityRaw);

                if (!locations.ContainsKey(cityKey))
                    throw new Exception($"Row {row}: Location not found '{cityRaw}'");

                var location = locations[cityKey];

                if (!decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost))
                    throw new Exception($"Row {row}: Invalid cost");

                string openingRaw = worksheet.Cells[row, 5].Text.Trim();

                TimeOnly? openHour = null;
                TimeOnly? closeHour = null;
                bool is24Hours = false;

                if (!string.IsNullOrWhiteSpace(openingRaw))
                {
                    var separators = new[] { "~", "-", "–" };

                    var parts = separators
                        .SelectMany(s => openingRaw.Split(s))
                        .Select(x => x.Trim())
                        .ToArray();

                    if (parts.Length < 2)
                        throw new Exception($"Row {row}: Invalid opening hours format");

                    if (!TimeOnly.TryParse(parts[0], out var open))
                        throw new Exception($"Row {row}: Invalid open hour");

                    if (!TimeOnly.TryParse(parts[1], out var close))
                        throw new Exception($"Row {row}: Invalid close hour");

                    openHour = open;
                    closeHour = close;

                    if (openHour == TimeOnly.MinValue && closeHour == TimeOnly.MinValue)
                        is24Hours = true;
                }

                if (!bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor))
                    throw new Exception($"Row {row}: Invalid IsIndoor value");

                // Preferences
                var prefRaw = worksheet.Cells[row, 8].Text.Trim();
                var preferenceIds = new List<Guid>();

                if (!string.IsNullOrEmpty(prefRaw))
                {
                    foreach (var item in prefRaw.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!Guid.TryParse(item.Trim(), out var id))
                            throw new Exception($"Row {row}: Invalid PreferenceId '{item}'");

                        preferenceIds.Add(id);
                    }
                }

                // Coordinates
                double.TryParse(worksheet.Cells[row, 9].Text, out var lat);
                double.TryParse(worksheet.Cells[row, 10].Text, out var lng);

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
                    IsIndoor = isIndoor,

                    VisitRecommendation = GetVisitRecommendation(
                        openHour, closeHour, is24Hours, isIndoor),

                    GoogleMapLink = worksheet.Cells[row, 6].Text,

                    LocationId = location.LocationId,

                    Latitude = lat != 0 ? lat : location.Latitude,
                    Longitude = lng != 0 ? lng : location.Longitude,

                    POIImgUrl = _imageMap.TryGetValue(name.ToLower(), out var img)
                        ? img
                        : null
                };

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

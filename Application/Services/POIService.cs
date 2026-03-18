using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
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
                        OpeningHours = poi.OpeningHours,
                        GoogleMapLink = poi.GoogleMapLink,
                        IsIndoor = poi.IsIndoor,
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

        public async Task<POI> CreateAsync(POI request)
        {
            var (lat, lon) = await _geocodingService
                .GetCoordinatesAsync(request.Name, request.City);

            var poi = new POI
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Address = request.Address,
                City = request.City,
                ApproxCost = request.ApproxCost,
                OpeningHours = request.OpeningHours,
                GoogleMapLink = request.GoogleMapLink,
                IsIndoor = request.IsIndoor,
                Latitude = lat,
                Longitude = lon
            };

            await _poiRepository.AddAsync(poi);

            return poi;
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

            if (request.OpeningHours != null)
                poi.OpeningHours = request.OpeningHours;

            if (request.GoogleMapLink != null)
                poi.GoogleMapLink = request.GoogleMapLink;

                poi.IsIndoor = request.IsIndoor;

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

            // 🔥 Load toàn bộ Location 1 lần (tránh gọi DB trong loop)
            var locations = (await _locationRepository.GetAllAsync())
              .GroupBy(x => x.LocationName.ToLower())
              .ToDictionary(g => g.Key, g => g.First());

            List<POI> pois = new();

            for (int row = 2; row <= rowCount; row++)
            {
                string name = worksheet.Cells[row, 1].Text.Trim();
                string address = worksheet.Cells[row, 2].Text.Trim();
                string cityRaw = worksheet.Cells[row, 3].Text.Trim();
                string cityKey = cityRaw.ToLower();

                if (!locations.ContainsKey(cityKey))
                    continue;

                var location = locations[cityKey];

                decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost);
                bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor);

                // 🔥 LẤY IMAGE TỪ MAP
                var normalizedName = name.Trim().ToLower();

                string? imageUrl = _imageMap.ContainsKey(normalizedName)
                    ? _imageMap[normalizedName]
                    : null;

                var poi = new POI
                {
                    Id = Guid.NewGuid(),

                    Name = name,
                    Address = address,
                    City = cityRaw,

                    ApproxCost = cost.ToString(),
                    OpeningHours = worksheet.Cells[row, 5].Text,
                    GoogleMapLink = worksheet.Cells[row, 6].Text,
                    IsIndoor = isIndoor,

                    LocationId = location.LocationId,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,

                    // 🔥 QUAN TRỌNG NHẤT
                    POIImgUrl = imageUrl
                };

                pois.Add(poi);
            }

            // 🔥 Save 1 lần (chuẩn clean + performance tốt)
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

    }
}

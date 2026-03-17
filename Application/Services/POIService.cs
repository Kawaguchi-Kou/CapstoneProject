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

        public POIService(
            IPOIRepository poiRepository,
            IUserRepository userRepository,
            IGeocodingService geocodingService)
        {
            _poiRepository = poiRepository;
            _userRepository = userRepository;
            _geocodingService = geocodingService;
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

        public async Task<List<POIResponse>> GetAllAsync()
        {
            var pois = await _poiRepository.GetAllAsync();

            return pois.Select(p => new POIResponse
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                City = p.City,
                ApproxCost = p.ApproxCost,
                OpeningHours = p.OpeningHours,
                GoogleMapLink = p.GoogleMapLink,
                IsIndoor = p.IsIndoor,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                LocationId = p.LocationId
            }).ToList();
        }

        public async Task<POIResponse?> GetByIdAsync(Guid id)
        {
            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                return null;

            return new POIResponse
            {
                Id = poi.Id,
                Name = poi.Name,
                Address = poi.Address,
                City = poi.City,
                ApproxCost = poi.ApproxCost,
                OpeningHours = poi.OpeningHours,
                GoogleMapLink = poi.GoogleMapLink,
                IsIndoor = poi.IsIndoor,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                LocationId = poi.LocationId
            };
        }

        public async Task<POI> CreateAsync(CreatePoiRequest request)
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

        public async Task<POI> UpdateAsync(Guid id, UpdatePoiRequest request)
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

            if (request.IsIndoor.HasValue)
                poi.IsIndoor = request.IsIndoor.Value;

            if (request.Latitude.HasValue)
                poi.Latitude = request.Latitude.Value;

            if (request.Longitude.HasValue)
                poi.Longitude = request.Longitude.Value;

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
            var locations = (await _poiRepository.GetAllLocationsAsync())
                .ToDictionary(x => x.LocationName.ToLower(), x => x);

            List<POI> pois = new();

            for (int row = 2; row <= rowCount; row++)
            {
                string name = worksheet.Cells[row, 1].Text.Trim();
                string address = worksheet.Cells[row, 2].Text.Trim();
                string cityRaw = worksheet.Cells[row, 3].Text.Trim();
                string cityKey = cityRaw.ToLower();

                // ❌ không có location thì bỏ
                if (!locations.ContainsKey(cityKey))
                    continue;

                var location = locations[cityKey];

                decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost);
                bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor);

                var poi = new POI
                {
                    Id = Guid.NewGuid(),

                    Name = name,
                    Address = address,
                    City = cityRaw, // giữ nguyên format đẹp

                    ApproxCost = cost.ToString(),
                    OpeningHours = worksheet.Cells[row, 5].Text,
                    GoogleMapLink = worksheet.Cells[row, 6].Text,
                    IsIndoor = isIndoor,

                    // 🔥 mapping từ Location
                    LocationId = location.LocationId,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                };

                pois.Add(poi);
            }

            // 🔥 Save 1 lần (chuẩn clean + performance tốt)
            await _poiRepository.AddRangeAsync(pois);
        }

    }
}

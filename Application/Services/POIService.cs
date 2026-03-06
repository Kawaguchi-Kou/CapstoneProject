using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

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
                .Select(poi => new
                {
                    Poi = poi,
                    Score = poi.PoiPreferences.Count(pp =>
                        pp.Preference != null &&
                        userPrefSet.Contains(pp.Preference.Name))
                })
                .OrderByDescending(x => x.Score)
                .Select(x => new RecommendedPoiResponse
                {
                    Id = x.Poi.Id,
                    Name = x.Poi.Name,
                    City = x.Poi.City,
                    Description = x.Poi.Description,
                    ApproxCost = x.Poi.ApproxCost,
                    Latitude = x.Poi.Latitude,
                    Longitude = x.Poi.Longitude,
                    Score = x.Score
                })
                .ToList();

            return result;
        }

        public async Task<List<PoiResponse>> GetAllAsync()
        {
            var pois = await _poiRepository.GetAllAsync();

            return pois.Select(p => new PoiResponse
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                Address = p.Address,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Description = p.Description
            }).ToList();
        }

        public async Task<PoiResponse?> GetByIdAsync(Guid id)
        {
            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                return null;

            return new PoiResponse
            {
                Id = poi.Id,
                Name = poi.Name,
                City = poi.City,
                Address = poi.Address,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                Description = poi.Description
            };
        }

        public async Task<Guid> CreateAsync(CreatePoiRequest request)
        {
            ValidateCreateRequest(request);

            var existing = await _poiRepository.GetByNameAndCityAsync(
                request.Name,
                request.City);

            if (existing != null)
                throw new Exception("POI already exists in this city");


            var (latitude, longitude) =
                await _geocodingService.GetCoordinatesAsync(
                    request.Name,
                    request.City);

            var poi = new POI
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                City = request.City,
                Address = request.Address,
                Latitude = latitude,
                Longitude = longitude,
                Description = request.Description
            };

            await _poiRepository.AddAsync(poi);

            return poi.Id;
        }

        public async Task UpdateAsync(Guid id, UpdatePoiRequest request)
        {

            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                throw new Exception("POI not found");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("POI name is required");

            if (string.IsNullOrWhiteSpace(request.City))
                throw new ArgumentException("City is required");

            if (poi == null)
                throw new Exception("POI not found");

            poi.Name = request.Name;
            poi.City = request.City;
            poi.Address = request.Address;
            poi.Latitude = request.Latitude;
            poi.Longitude = request.Longitude;
            poi.Description = request.Description;

            await _poiRepository.UpdateAsync(poi);
        }

        public async Task DeleteAsync(Guid id)
        {
            var poi = await _poiRepository.GetByIdAsync(id);

            if (poi == null)
                throw new Exception("POI not found");

            await _poiRepository.DeleteAsync(poi);
        }

        private void ValidateCreateRequest(CreatePoiRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("POI name is required");

            if (string.IsNullOrWhiteSpace(request.City))
                throw new ArgumentException("City is required");

            if (string.IsNullOrWhiteSpace(request.Address))
                throw new ArgumentException("Address is required");

            if (request.Description?.Length > 1000)
                throw new ArgumentException("Description cannot exceed 1000 characters");
        }

    }

}

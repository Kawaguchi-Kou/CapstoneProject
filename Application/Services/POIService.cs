using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Constants;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services
{
    public class POIService : IPOIService
    {
        private readonly IPOIRepository _poiRepository;
        private readonly IUserRepository _userRepository;

        public POIService(
            IPOIRepository poiRepository,
            IUserRepository userRepository)
        {
            _poiRepository = poiRepository;
            _userRepository = userRepository;
        }

        public async Task<List<POIScoreResult>> CalculateScoresAsync(Guid accountId)
        {
            // 1. User preferences
            var userPrefs = await _userRepository.GetByAccountIdAsync(accountId);

            var userPrefSet = userPrefs
                .Select(x => x.PreferenceCode)
                .ToHashSet();

            // 2. All POIs
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            var results = new List<POIScoreResult>();

            foreach (var poi in pois)
            {
                int score = poi.PoiPreferences.Count(pp =>
                    pp.Preference != null &&
                    userPrefSet.Contains(pp.Preference.Name));

                results.Add(new POIScoreResult
                {
                    PoiId = poi.Id,
                    PoiName = poi.Name,
                    Score = score
                });
            }

            return results
                .OrderByDescending(x => x.Score)
                .ToList();
        }

        public async Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(
    Guid accountId)
        {
            var userPrefs = await _userRepository.GetByAccountIdAsync(accountId);
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            var userPrefSet = userPrefs
                .Select(x => x.PreferenceCode)
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

    }

}

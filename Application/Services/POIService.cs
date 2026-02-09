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
            var userPrefs = await _userRepository.GetByAccountIdAsync(accountId);
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            // Map: PreferenceName → UserScore
            var userPrefDict = userPrefs.ToDictionary(
                x => x.PreferenceCode, // ở UserPreferenceVector bạn đang dùng string này
                x => x.Score
            );

            var results = new List<POIScoreResult>();

            foreach (var poi in pois)
            {
                double totalScore = 0;

                foreach (var poiPref in poi.PoiPreferences)
                {
                    var preferenceName = poiPref.Preference?.Name;
                    if (preferenceName == null)
                        continue;

                    if (!userPrefDict.TryGetValue(preferenceName, out var userScore))
                        continue;

                    var poiWeight = poiPref.Weight; // 0 → 1
                    var systemWeight = PreferenceWeights.Get(preferenceName);

                    totalScore += userScore * poiWeight * systemWeight;
                }

                results.Add(new POIScoreResult
                {
                    PoiId = poi.Id,
                    PoiName = poi.Name,
                    Score = Math.Round(totalScore, 4)
                });
            }

            return results
                .OrderByDescending(x => x.Score)
                .ToList();
        }

        public async Task<List<RecommendedPoiResponse>> GetRecommendedPoisAsync(
        Guid accountId,
        int limit = 10)
        {
            var scores = await CalculateScoresAsync(accountId);
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            var poiDict = pois.ToDictionary(x => x.Id);

            var result = scores
                .Where(s => poiDict.ContainsKey(s.PoiId))
                .OrderByDescending(s => s.Score)
                .Take(limit)
                .Select(s =>
                {
                    var poi = poiDict[s.PoiId];
                    return new RecommendedPoiResponse
                    {
                        Id = poi.Id,
                        Name = poi.Name,
                        City = poi.City,
                        Description = poi.Description,
                        ApproxCost = poi.ApproxCost,
                        Latitude = poi.Latitude,
                        Longitude = poi.Longitude,
                        Score = s.Score
                    };
                })
                .ToList();

            return result;
        }
    }

}

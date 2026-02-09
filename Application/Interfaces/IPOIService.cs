using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;

namespace Application.Interfaces
{
    public interface IPOIService
    {
        Task<List<POIScoreResult>> CalculateScoresAsync(Guid accountId);
        Task<List<RecommendedPoiResponse>> GetRecommendedPoisAsync(
        Guid accountId,
        int limit = 10);
    }
}

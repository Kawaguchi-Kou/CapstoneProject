using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;

namespace Application.Interfaces
{
    public interface IPOIService
    {
        //Task<List<POIScoreResult>> CalculateScoresAsync(Guid accountId);
        Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(Guid accountId);
        Task<List<PoiResponse>> GetAllAsync();
        Task<PoiResponse?> GetByIdAsync(Guid id);
        Task<Guid> CreateAsync(CreatePoiRequest request);
        Task UpdateAsync(Guid id, UpdatePoiRequest request);
        Task DeleteAsync(Guid id);
    }
}

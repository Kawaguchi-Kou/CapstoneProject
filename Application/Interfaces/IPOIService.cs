using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IPOIService
    {
        //Task<List<POIScoreResult>> CalculateScoresAsync(Guid accountId);
        Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(Guid accountId);

        Task<List<POI>> GetAllAsync();
        Task<POI?> GetByIdAsync(Guid id);
        Task<POI> CreateAsync(POI request, List<Guid> preferenceIds);
        Task<POI> UpdateAsync(Guid id, POI request);

        Task DeleteAsync(Guid id);

        Task ImportExcelAsync(IFormFile file);
        void AddImageMapping(string fileName, string url);

    }
}

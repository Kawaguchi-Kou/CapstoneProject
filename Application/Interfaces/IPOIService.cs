using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IPOIService
    {
        Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(Guid accountId);

        Task<List<POI>> GetAllAsync();
        Task<POI?> GetByIdAsync(Guid id);
        Task<POI> CreateAsync(POI request, List<Guid> preferenceIds, Guid locationId, Guid districtId);
        Task<POI> CreatePartnerPoiAsync(Guid partnerId, POI request, List<Guid> preferenceIds);
        Task<POI> UpdateAsync(Guid id, POI request);
        Task<POI> UpdatePartnerPoiAsync(Guid partnerId, Guid id, POI request);

        Task<List<POI>> GetMyPoisAsync(Guid partnerId);
        Task<PagedResultResponse<POI>> GetMyPoisAsync(Guid partnerId, int page, int pageSize);
        Task<POI?> GetMyPoiByIdAsync(Guid partnerId, Guid poiId);
        Task<List<POI>> GetPendingPartnerPoisAsync();
        Task<PagedResultResponse<POI>> GetPendingPartnerPoisAsync(int page, int pageSize);
        Task<POI> ApprovePartnerPoiAsync(Guid poiId);
        Task<POI> RejectPartnerPoiAsync(Guid poiId);
        Task<(POI poi, int affectedAds)> InactivatePoiAsync(Guid actorId, Guid poiId, bool isManagerOrStaff, bool confirmCascade);
        Task<POI> ActivatePoiAsync(Guid poiId);

        Task ImportExcelAsync(IFormFile file);
        void AddImageMapping(string fileName, string url);
    }
}

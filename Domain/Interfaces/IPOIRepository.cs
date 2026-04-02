using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPOIRepository
    {
        Task<POI?> GetByIdAsync(Guid poiId);
        Task<List<POI>> GetAllWithPreferencesAsync();
        Task<List<POI>> GetAllAsync();
        Task<List<POI>> GetByPartnerIdAsync(Guid partnerId);
        Task<List<POI>> GetByPartnerIdAsync(Guid partnerId, int skip, int take);
        Task<int> CountByPartnerIdAsync(Guid partnerId);
        Task<List<POI>> GetPendingPartnerPoisAsync();
        Task<List<POI>> GetPendingPartnerPoisAsync(int skip, int take);
        Task<int> CountPendingPartnerPoisAsync();
        Task AddAsync(POI poi, List<Guid> preferenceIds);
        Task AddRangeAsync(List<POI> pois);

        Task<Location?> GetLocationByIdAsync(Guid locationId);
        Task UpdateAsync(POI poi);
        Task<POI?> GetByNameAndCityAsync(string name, string city);
        Task<List<POI>> GetByLocationAsync(Guid locationId);
    }
}

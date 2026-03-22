using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IItineraryDetailRepository
    {
        Task<List<ItineraryDetail>> GetByItineraryIdAsync(Guid itineraryId);

        Task<ItineraryDetail?> GetByIdAsync(Guid id);

        Task AddAsync(ItineraryDetail detail);

        Task AddRangeAsync(List<ItineraryDetail> details);

        Task UpdateAsync(ItineraryDetail detail);

        Task UpdateRangeAsync(List<ItineraryDetail> details);

        Task DeleteByItineraryIdAsync(Guid itineraryId);
        Task<List<ItineraryDetail>> GetHighRiskDetailsAsync(Guid itineraryId, double threshold);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IItineraryRepository
    {
        Task<Itinerary?> GetBySegmentIdAsync(Guid segmentId);

        Task<Itinerary?> GetByIdAsync(Guid id);

        Task AddAsync(Itinerary itinerary);

        Task UpdateAsync(Itinerary itinerary);

        Task DeleteAsync(Guid id);
    }
}

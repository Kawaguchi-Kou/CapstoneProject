using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITripSegmentRepository
    {
        Task<List<TripSegment>> GetByTripIdAsync(Guid tripId);

        Task<TripSegment?> GetByIdAsync(Guid id);

        Task AddAsync(TripSegment segment);

        Task AddRangeAsync(List<TripSegment> segments);

        Task UpdateAsync(TripSegment segment);

        Task DeleteAsync(Guid id);
    }
}

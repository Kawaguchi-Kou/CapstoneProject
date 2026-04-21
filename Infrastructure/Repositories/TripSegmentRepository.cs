using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TripSegmentRepository : ITripSegmentRepository
    {
        private readonly AppDbContext _context;

        public TripSegmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TripSegment>> GetByTripIdAsync(Guid tripId)
        {
            return await _context.TripSegments
                .Where(x => x.TripId == tripId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();
        }

        public async Task<TripSegment?> GetByIdAsync(Guid id)
        {
            return await _context.TripSegments
                .FirstOrDefaultAsync(x => x.SegmentId == id);
        }

        public async Task AddAsync(TripSegment segment)
        {
            await _context.TripSegments.AddAsync(segment);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<TripSegment> segments)
        {
            await _context.TripSegments.AddRangeAsync(segments);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TripSegment segment)
        {
            _context.TripSegments.Update(segment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.TripSegments.FindAsync(id);
            if (entity != null)
            {
                _context.TripSegments.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByIdsAsync(List<Guid> segmentIds)
        {
            var segments = await _context.TripSegments
                .Where(x => segmentIds.Contains(x.SegmentId))
                .ToListAsync();

            _context.TripSegments.RemoveRange(segments);
        }
    }
}

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
    public class ItineraryRepository : IItineraryRepository
    {
        private readonly AppDbContext _context;

        public ItineraryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Itinerary?> GetBySegmentIdAsync(Guid segmentId)
        {
            return await _context.Itineraries
                .FirstOrDefaultAsync(x => x.SegmentId == segmentId);
        }

        public async Task<Itinerary?> GetByIdAsync(Guid id)
        {
            return await _context.Itineraries
                .FirstOrDefaultAsync(x => x.ItineraryId == id);
        }

        public async Task AddAsync(Itinerary itinerary)
        {
            await _context.Itineraries.AddAsync(itinerary);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Itinerary itinerary)
        {
            _context.Itineraries.Update(itinerary);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Itineraries.FindAsync(id);
            if (entity != null)
            {
                _context.Itineraries.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddRangeAsync(List<Itinerary> itineraries)
        {
            if (itineraries == null || !itineraries.Any())
                return;

            await _context.Itineraries.AddRangeAsync(itineraries);
        }
    }
}

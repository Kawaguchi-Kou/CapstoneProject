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
    public class PlannerRepository : IPlannerRepository
    {
        private readonly AppDbContext _context;

        public PlannerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Trip?> GetTripWithSegmentsAndItinerary(Guid tripId)
        {
            return await _context.Trips
                .Include(t => t.TripSegments)
                .Include(t => t.Itineraries)
                    .ThenInclude(i => i.ItineraryDetails)
                .FirstOrDefaultAsync(t => t.TripId == tripId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

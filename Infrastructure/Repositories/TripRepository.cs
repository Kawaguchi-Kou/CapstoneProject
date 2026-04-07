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
    public class TripRepository : ITripRepository
    {
        private readonly AppDbContext _db;

        public TripRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Trip?> GetFullTripAsync(Guid tripId)
        {
            return await _db.Trips!
                .Include(t => t.TripSegments!)
                    .ThenInclude(s => s.Itineraries!)
                        .ThenInclude(i => i.ItineraryDetails!)
                .Include(t => t.TripSegments!)
                    .ThenInclude(s => s.Itineraries!)
                        .ThenInclude(i => i.ItineraryDetails!)
                            .ThenInclude(d => d.POI)
                .FirstOrDefaultAsync(x => x.TripId == tripId);
        }

        public async Task AddAsync(Trip trip)
        {
            _db.Trips.Add(trip);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Trip>> GetUpcomingTripsAsync(DateTime fromDate)
        {
            return await _db.Trips
                .Where(t => t.StartDate >= fromDate)
                .ToListAsync();
        }

        public async Task<Trip?> GetByIdAsync(Guid tripId)
        {
            return await _db.Trips.FindAsync(tripId);
        }
    }
}

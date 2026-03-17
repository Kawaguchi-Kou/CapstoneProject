using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITripRepository
    {
        Task<Trip?> GetFullTripAsync(Guid tripId);

        Task AddAsync(Trip trip);

        Task<List<Trip>> GetUpcomingTripsAsync(DateOnly fromDate);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITripService
    {
        Task<Trip> CreateTripAsync(Trip newTrip, Guid startDistrictId, Guid endDistrictId);
        Task<List<Trip>> GetUserTrips();
    }
}

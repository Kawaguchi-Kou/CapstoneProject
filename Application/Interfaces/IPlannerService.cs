using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPlannerService
    {
        Task GenerateAsync(Guid tripId);
        //Task<PlannerResponse> ReplanAsync(Guid tripId);
        //Task UpdateItineraryDetail(Guid detailId, UpdateDetailRequest dto);
        Task<List<TripSegment>> GetByTripIdWithDetailsAsync(Guid tripId);

        /// <summary>
        /// Fetches and caches fresh OpenMeteo weather for every segment of the given trip.
        /// Call this after trip creation and when the user requests a weather refresh.
        /// </summary>
        Task PreloadTripWeatherAsync(Guid tripId);
    }
}

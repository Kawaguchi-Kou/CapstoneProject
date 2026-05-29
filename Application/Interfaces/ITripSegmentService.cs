using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITripSegmentService
    {
        Task<List<Location>> RecommendSegmentsAsync(
            DateTime startDate,
            DateTime endDate,
            int maxStops);

        //Task<List<TripSegment>> AddSegmentsToTripAsync(Guid tripId, List<TripSegment> segments);

        Task<List<TripSegment>> InsertSegmentsAsync(
    Guid tripId,
    int insertAt,
    List<TripSegment> newSegments);

        Task<List<Location>> GetAllAsync();
        Task DeleteSegmentsAsync(
    Guid tripId,
    List<Guid> segmentIds);
        Task UpdateSegmentDatesAsync(
    Guid tripId,
    List<UpdateSegmentDatesRequest> updates);

        /// <summary>
        /// Finds the top-5 shortest routes between the trip's start and end location
        /// using the Vietnam travel graph, fetches weather data for each stop,
        /// and returns an AI-generated recommendation in Vietnamese per route.
        /// </summary>
        Task<RouteSuggestionResponse>
    GetRouteSuggestionAsync(
        Guid tripId,
        string routeId);
        Task ApplyRouteAsync(
    Guid tripId,
    RouteOptionDTO selectedRoute);

        Task<List<RouteOptionDTO>>
    GetAvailableRoutesAsync(Guid tripId);

        Task UpdateSegmentAsync(
    Guid tripId,
    Guid segmentId,
    TripSegment updatedSegment);
    }
}

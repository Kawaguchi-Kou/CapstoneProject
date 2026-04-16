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
    }
}

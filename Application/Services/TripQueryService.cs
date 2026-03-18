using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain.Interfaces;

namespace Application.Services
{
    public class TripQueryService : ITripQueryService
    {
        private readonly ITripRepository _repo;
        private readonly IMapper _mapper;

        public TripQueryService(ITripRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<TripRiskContextResponse> GetRiskContextAsync(Guid tripId)
        {
            var trip = await _repo.GetFullTripAsync(tripId);
            var response = _mapper.Map<TripRiskContextResponse>(trip);
            return response;
        }
    }
}

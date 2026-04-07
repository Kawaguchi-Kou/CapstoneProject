using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/trip")]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly ITripSegmentService _tripSegmentService;

        public TripController(ITripService tripService, IAuthService authService, IMapper mapper, ITripSegmentService tripSegmentService)
        {
            _tripService = tripService;
            _authService = authService;
            _mapper = mapper;
            _tripSegmentService = tripSegmentService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] TripRequest request,
    [FromQuery] TripType type)
        {
            try
            {
                var account = await _authService.GetCurrentAccount();
                var accountId = account.Id;

                var trip = _mapper.Map<Trip>(request);
                trip.TripType = type;
                trip.OwnerId = accountId;
                await _tripService.CreateTripAsync(trip);

                var result = _mapper.Map<TripResponse>(trip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{tripId}/segments")]
        public async Task<IActionResult> AddSegments(
        Guid tripId,
        [FromQuery] int insertAt,
        [FromBody] List<AddTripSegmentRequest> requests)
        {
            //// DTO → Entity
            //var segments = _mapper.Map<List<TripSegment>>(requests);

            //// Call service
            //var result = await _tripSegmentService.AddSegmentsToTripAsync(tripId, segments);

            //// Entity → DTO
            //var response = _mapper.Map<List<TripSegmentResponse>>(result);

            //return Ok(response);

            try
            {
                var segments = _mapper.Map<List<TripSegment>>(requests);

                var result = await _tripSegmentService
                    .InsertSegmentsAsync(tripId, insertAt, segments);

                var response = _mapper.Map<List<TripSegmentResponse>>(result);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

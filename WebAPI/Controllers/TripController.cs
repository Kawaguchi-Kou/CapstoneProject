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
        [FromBody] List<AddTripSegmentRequest> requests)
        {
            // DTO → Entity
            var segments = _mapper.Map<List<TripSegment>>(requests);

            // Call service
            var result = await _tripSegmentService.AddSegmentsToTripAsync(tripId, segments);

            // Entity → DTO
            var response = _mapper.Map<List<TripSegmentResponse>>(result);

            return Ok(response);
        }

        [HttpPost("{tripId}/share")]
        [Authorize]
        public async Task<IActionResult> GenerateShareLink(Guid tripId, [FromQuery] string frontendBaseUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(frontendBaseUrl))
                {
                    return BadRequest("Frontend base URL is required");
                }

                var account = await _authService.GetCurrentAccount();
                var result = await _tripService.GenerateShareLinkAsync(frontendBaseUrl, tripId, account.Id);

                return Ok(new
                {
                    inviteUrl = result.InviteUrl,
                    qrCodeBase64 = result.QrCodeBase64
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("join/{token}")]
        [Authorize]
        public async Task<IActionResult> JoinTrip(string token)
        {
            try
            {
                var account = await _authService.GetCurrentAccount();
                await _tripService.JoinTripAsync(token, account.Id);

                return Ok(new { message = "Successfully joined the trip" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{tripId}/role")]
        [Authorize]
        public async Task<IActionResult> GetUserRoleInTrip(Guid tripId)
        {
            try
            {
                var account = await _authService.GetCurrentAccount();
                var role = await _tripService.GetUserRoleInTripAsync(tripId, account.Id);

                if (role == null)
                {
                    return Ok(new { role = "None" });
                }

                return Ok(new { role = role.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

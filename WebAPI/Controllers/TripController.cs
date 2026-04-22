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
        private readonly IParticipantService _service;

        public TripController(ITripService tripService, IAuthService authService, IMapper mapper, ITripSegmentService tripSegmentService, IParticipantService participantService)
        {
            _tripService = tripService;
            _authService = authService;
            _mapper = mapper;
            _tripSegmentService = tripSegmentService;
            _service = participantService;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] TripRequest request)
        {
            try
            {
                var account = await _authService.GetCurrentAccount();
                var accountId = account.Id;

                var trip = _mapper.Map<Trip>(request);
                trip.TripType = TripType.OneWay;
                trip.OwnerId = accountId;
                await _tripService.CreateTripAsync(trip, request.StartDistrictId, request.EndDistrictId);

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
                Console.WriteLine(requests.GetType());
                Console.WriteLine(requests.First().GetType());
                var segments = _mapper.Map<List<TripSegment>>(requests);

                var result = await _tripSegmentService
                    .InsertSegmentsAsync(tripId, insertAt, segments);

                var response = _mapper.Map<List<TripSegmentResponse>>(result);

                return Ok(response);
            }
            catch (Exception ex)
            {
                //return BadRequest(ex.Message);
                return BadRequest(new
                {
                    ex.Message,
                    Inner = ex.InnerException?.Message,
                    Stack = ex.StackTrace
                });
            }
        }

        // 🔹 1. Update segment dates
        [HttpPut("{tripId}/segments/update-dates")]
        public async Task<IActionResult> UpdateSegmentDates(
            Guid tripId,
            [FromBody] List<UpdateSegmentDatesRequest> request)
        {
            try
            {
                await _tripSegmentService.UpdateSegmentDatesAsync(tripId, request);

                return Ok(new
                {
                    message = "Segment dates updated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    detail = ex.Message
                });
            }
        }

        // 🔹 2. Delete segments
        [HttpDelete("{tripId}/segments")]
        public async Task<IActionResult> DeleteSegments(
            Guid tripId,
            [FromBody] List<Guid> segmentIds)
        {
            try
            {
                await _tripSegmentService.DeleteSegmentsAsync(tripId, segmentIds);

                return Ok(new
                {
                    message = "Segments deleted successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("get-all-location")]
        public async Task<IActionResult> GetAllLocation()
        {
            try
            {
                var locations = await _tripSegmentService.GetAllAsync();
                var resposne = _mapper.Map<List<LocationResponse>>(locations);
                return Ok(resposne);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 🔷 Owner add
        [HttpPost("{tripId}/participants")]
        public async Task<IActionResult> AddParticipant(Guid tripId, AddParticipantRequest request)
        {
            var user = await _authService.GetCurrentAccount();

            var participant = await _service.AddTripParticipantAsync(tripId, request, user.Id);

            return Ok(_mapper.Map<ParticipantResponse>(participant));
        }

        [HttpGet("{tripId}/get-participants")]
        public async Task<IActionResult> GetParticipant(Guid tripId)
        {
            try
            {
                var participant = await _service.GetAllParticipantByTripIdAsync(tripId);
                var response = _mapper.Map<List<ParticipantResponse>>(participant);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 🔷 Generate link
        [HttpGet("{tripId}/invite-link")]
        public async Task<IActionResult> GenerateInvite(Guid tripId)
        {
            var user = await _authService.GetCurrentAccount();

            var link = await _service.GenerateInviteLinkAsync(tripId, user.Id);

            return Ok(new { inviteUrl = link });
        }

        [HttpGet("{tripId}/generate-qr")]
        public async Task<IActionResult> GenerateQR(Guid tripId)
        {
            var user = await _authService.GetCurrentAccount();

            var (link, qrImage) = await _service.GenerateInviteQrAsync(tripId, user.Id);

            return Ok(new
            {
                inviteUrl = link,
                qrCode = Convert.ToBase64String(qrImage) // so FE can render it
            });
        }

        // 🔷 Join via QR
        [HttpPost("/api/invites/join")]
        public async Task<IActionResult> Join([FromQuery] Guid tripId)
        {
            var user = await _authService.GetCurrentAccount();

            var participant = await _service.JoinByTripIdAsync(tripId, user.Id);

            return Ok(_mapper.Map<ParticipantResponse>(participant));
        }
    }
}

using System.Security.Claims;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/advertisements")]
    [Authorize]
    public class AdvertisementController : ControllerBase
    {
        private readonly IAdvertisementService _advertisementService;
        private readonly IMapper _mapper;

        public AdvertisementController(IAdvertisementService advertisementService, IMapper mapper)
        {
            _advertisementService = advertisementService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdvertisement([FromBody] CreateAdvertisementRequest request)
        {
            try
            {
                // Kiểm tra role Partner
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Partner")
                {
                    return StatusCode(403, new { 
                        message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Partner.", 
                        role = userRole 
                    });
                }

                // Lấy accountId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var advertisement = await _advertisementService.CreateAdvertisementAsync(accountId, request);
                var response = _mapper.Map<AdvertisementResponse>(advertisement);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var advertisement = await _advertisementService.GetByIdAsync(id);
                if (advertisement == null)
                    return NotFound(new { message = "Advertisement not found" });

                var response = _mapper.Map<AdvertisementResponse>(advertisement);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-ads")]
        public async Task<IActionResult> GetMyAdvertisements()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var accountId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                var advertisements = await _advertisementService.GetByAccountIdAsync(accountId);
                var responses = _mapper.Map<List<AdvertisementResponse>>(advertisements);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveAdvertisement(Guid id)
        {
            try
            {
                var advertisement = await _advertisementService.ApproveAdvertisementAsync(id);
                var response = _mapper.Map<AdvertisementResponse>(advertisement);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectAdvertisement(Guid id, [FromBody] RejectAdvertisementRequest? request = null)
        {
            try
            {
                var advertisement = await _advertisementService.RejectAdvertisementAsync(id, request?.Reason);
                var response = _mapper.Map<AdvertisementResponse>(advertisement);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAllAdvertisements()
        {
            try
            {
                var advertisements = await _advertisementService.GetAllAsync();
                var response = _mapper.Map<List<AdvertisementResponse>>(advertisements);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetPendingAdvertisements()
        {
            try
            {
                var advertisements = await _advertisementService.GetPendingAsync();
                var response = _mapper.Map<List<AdvertisementResponse>>(advertisements);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/approve-staff")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> ApproveAd(Guid id)
        {
            try
            {
                var advertisement = await _advertisementService.ApproveAdAsync(id);
                var response = _mapper.Map<AdvertisementResponse>(advertisement);

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}

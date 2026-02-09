using System.Security.Claims;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Azure.Core;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IMapper mapper, ICloudinaryService cloudinaryService, IAuthService authService)
        {
            _userService = userService;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _authService = authService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _userService.GetById(id);
                var userResponse = _mapper.Map<UserResponse>(user);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> GetByIds([FromBody] List<Guid> ids)
        {
            try
            {
                var users = await _userService.GetByIdsAsync(ids);

                var userResponses = _mapper.Map<List<UserResponse>>(users);

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userService.GetAll();
                var userResponses = _mapper.Map<List<UserResponse>>(users);
                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token: User ID not found" });
                }

                string? avatarUrl = null;

                try
                {
                    if (request.AvatarUrl != null && request.AvatarUrl.Length > 0)
                    {
                        using var stream = request.AvatarUrl.OpenReadStream();
                        avatarUrl = await _cloudinaryService.UploadImageAsync(stream, request.AvatarUrl.FileName);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"File upload failed: {ex.Message}");
                }

                var user = _mapper.Map<Account>(request);
                user.AvatarUrl = avatarUrl!;
                user.Id = userId; // Ensure the user ID is set from the token
                var updatedUser = await _userService.UpdateProfile(user);
                var userResponse = _mapper.Map<UserResponse>(updatedUser);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("update-preference-vector")]
        [Authorize]
        public async Task<IActionResult> UpdatePreferences(
        [FromBody] UserPreferencesRequest request)
        {
            try
            {
                var account = _authService.GetCurrentAccount();
                var accountId = account.Result.Id;

                await _userService.UpdateUserPreferencesAsync(accountId, request);

                return Ok(new { message = "Preferences updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user-preferences")]
        [Authorize]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                var accountId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var result = await _userService.GetUserPreferencesAsync(accountId);

                return Ok(result.Select(x => new
                {
                    x.PreferenceCode,
                    x.Score
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

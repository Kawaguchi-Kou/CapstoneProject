using System.Security.Claims;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, IRefreshTokenService refreshTokenService,
                              IUserService userService, IMapper mapper)
        {
            _authService = authService;
            _refreshTokenService = refreshTokenService;
            _userService = userService;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerDto)
        {
            try
            {
                var account = _mapper.Map<Account>(registerDto);
                var accountId = Guid.NewGuid();
                account.Id = accountId;
                await _authService.RegisterAsync(account);

                //Tạo Profile User trong UserService qua ServiceClient
                var profile = _mapper.Map<CreateProfileRequest>(registerDto);
                profile.Id = accountId;

                var user = _mapper.Map<User>(profile);
                await _userService.CreateProfile(user);

                return Ok(new { Message = "Đã gửi OTP về email của bạn" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });

            }
        }


        [HttpPost("verify-register-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] OtpRequest dto)
        {
            try
            {
                var success = await _authService.GetValidRegisterOtpAsync(dto.Email, dto.OtpCode, "Register");
                if (!success) return BadRequest(new { Message = "OTP không hợp lệ hoặc đã hết hạn" });
                return Ok(new { Message = "Tài khoản đã được kích hoạt" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpGet("resend-register-otp")]
        public async Task<IActionResult> ResendRegisterOtp(string email)
        {
            try
            {
                await _authService.ResendRegisterOtpAsync(email);
                return Ok(new { Message = "Đã gửi lại mã OTP về email của bạn" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset(RequestPassResetRequest request)
        {
            try
            {
                await _authService.SendResetPasswordOtpAsync(request.Email);
                return Ok(new { message = "Đã gửi mã OTP về email của bạn" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost("verify-reset-password-otp")]
        public async Task<IActionResult> VerifyResetPasswordOtp([FromBody] OtpRequest dto)
        {
            try
            {
                var token = await _authService.GetValidResetPasswordOtpAsync(dto.Email, dto.OtpCode, "ResetPassword");
                return Ok(new { resetToken = token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request.resetToken, request.NewPassword);
                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                // 1. Lấy userId từ JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("Invalid token");

                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized("Invalid user id in token");

                // 2. Tìm user trong DB
                var user = await _authService.GetByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                // Verify khi login/đổi mật khẩu
                bool valid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
                if (!valid)
                    return BadRequest("Old password is incorrect");

                // 4. Hash mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // 5. Cập nhật DB
                await _authService.ChangePasswordAsync(user);

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {
            try
            {
                // 1. Lấy userId từ JWT
                var token = _refreshTokenService.GetByTokenAsync(refreshToken);
                var accountId = token.Result.AccountId.ToString();

                if (string.IsNullOrEmpty(accountId))
                    return Unauthorized("Invalid token");

                // 2. Tìm user trong DB
                var user = await _authService.GetByIdAsync(Guid.Parse(accountId));

                if (user == null)
                    return NotFound("User not found");

                var (newAccessToken, newRefreshToken) = await _authService.RefreshAsync(refreshToken, user);

                return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var user = await _authService.GetCurrentAccount();
                var userResponse = _mapper.Map<AccountResponse>(user);

                var profile = await _userService.GetById(user.Id);
                userResponse.Profile = _mapper.Map<ProfileResponse>(profile);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("account-by-email")]
        public async Task<IActionResult> GetAccountByEmail([FromQuery] string email)
        {
            try
            {
                var account = await _authService.GetByEmailAsync(email);
                if (account == null)
                    return NotFound(new { message = "Email không hợp lệ" });

                var response = _mapper.Map<AccountResponse>(account);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var accounts = await _authService.GetAllAccountsAsync();
                var profile = new ProfileResponse();
                var responses = _mapper.Map<List<AccountResponse>>(accounts);
                foreach (var response in responses)
                {
                    profile = _mapper.Map<ProfileResponse>(await _userService.GetById(response.Id));
                    response.Profile = profile;
                }
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

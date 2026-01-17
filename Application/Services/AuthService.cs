using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthServices: IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthServices(IAuthRepository authRepository, IConfiguration configuration, 
            IOtpRepository optRepository, IEmailService emailService,
            IRefreshTokenRepository refreshTokenRepository, IHttpContextAccessor httpContextAccessor)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _otpRepository = optRepository;
            _emailService = emailService;
            _refreshTokenRepository = refreshTokenRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegisterAsync(Account newAccount)
        {
            var existingUser = await _authRepository.GetByEmailAsync(newAccount.Email);
            if (existingUser != null)
                throw new Exception("Email đã tồn tại");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newAccount.PasswordHash);

            var account = new Account 
            { 
                Id = newAccount.Id,
                Email = newAccount.Email, 
                PasswordHash = passwordHash,
                IsActive = false,
                RoleId = 2 // Mặc định role user
            };
            await _authRepository.AddAsync(account);
            await _authRepository.SaveChangesAsync();

            // Tạo OTP
            var otpEntity = GenerateOtp(newAccount.Email, "Register");
            
            await _otpRepository.AddAsync(otpEntity);
            await _otpRepository.SaveChangesAsync();

            // Gửi email OTP
            await SendOtpEmail(account.Email, otpEntity.OtpCode);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest loginDto)
        {
            var account = await _authRepository.GetByEmailAsync(loginDto.Email);

            if (account == null || BCrypt.Net.BCrypt.Verify(loginDto.Password, account.PasswordHash) == false)
                throw new InvalidOperationException("Email hoặc password sai");

            if (account!.IsActive == false)
                throw new Exception("Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để xác nhận tài khoản.");

            var token = GenerateJwtToken(loginDto.Email).Result;

            // Tạo refresh token
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                InitialLoginAt = DateTime.UtcNow,
                AccountId = account.Id,
                IsRevoked = false
            };
            await _refreshTokenRepository.CreateTokenAsync(refreshToken);

            int expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

            return new AuthResponse
            {
                Token = token,
                UserName = account.Email,
                ExpiresAt = DateTime.Now.AddMinutes(expiryMinutes),
                RefreshToken = refreshToken.Token
            };
        }

        private async Task<string> GenerateJwtToken(string email)
        {
            var account = await _authRepository.GetByEmailAsync(email);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, account.Role.Name.ToString())
            };

            int expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private OtpVerification GenerateOtp(string email, string purpose)
        {
            var newOtp = new OtpVerification
            {
                Email = email,
                OtpCode = new Random().Next(100000, 999999).ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Purpose = purpose
            };
            return newOtp;
        }

        private async Task SendOtpEmail(string email, string otp)
        {
            var subject = "Mã OTP xác nhận tài khoản";
            var body = $"Mã OTP của bạn là <b>{otp}</b>. Hạn dùng: 10 phút.";
            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task ResendRegisterOtpAsync(string email)
        {
            var user = await _authRepository.GetByEmailAsync(email);
            if (user == null) throw new Exception("Email không tồn tại");

            // Inactivate all previous OTPs for this email and purpose
            var existingOtps = await _otpRepository.GetAllByAccountAsync(email, "Register");
            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
            }

            var newOtp = GenerateOtp(email, "Register");

            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync();

            string subject = "OTP Register";
            string body = $"Mã OTP của bạn là: <b>{newOtp.OtpCode}</b>. Hết hạn trong 10 phút.";
            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<bool> GetValidRegisterOtpAsync(string email, string otpCode, string purpose)
        {
            var otp = await _otpRepository.GetValidOtpAsync(email, otpCode, purpose);
            if (otp == null)
                return false;

            otp.IsUsed = true;
            await _otpRepository.SaveChangesAsync();

            // Kích hoạt user
            var user = await _authRepository.GetByEmailAsync(email);
            if (user != null) user.IsActive = true;
            await _authRepository.SaveChangesAsync();

            return true;
        }

        public async Task SendResetPasswordOtpAsync(string email)
        {
            var user = await _authRepository.GetByEmailAsync(email);
            if (user == null) throw new Exception("Email không tồn tại");

            // Inactivate all previous OTPs for this email and purpose
            var existingOtps = await _otpRepository.GetAllByAccountAsync(email, "ResetPassword");
            foreach (var otp in existingOtps)
            {
               otp.IsUsed = true;
            }

            var newOtp = GenerateOtp(email, "ResetPassword");

            await _otpRepository.AddAsync(newOtp);
            await _otpRepository.SaveChangesAsync();

            string subject = "OTP Reset Password";
            string body = $"Mã OTP của bạn là: <b>{newOtp.OtpCode}</b>. Hết hạn trong 10 phút.";
            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<string> GetValidResetPasswordOtpAsync(string email, string otpCode, string purpose)
        {
            var otp = await _otpRepository.GetValidOtpAsync(email, otpCode, purpose);
            if (otp == null)
                throw new Exception("OTP không hợp lệ hoặc đã hết hạn");

            otp.IsUsed = true;
            await _otpRepository.SaveChangesAsync();

            var resetToken = GenerateJwtToken(email).Result;

            var user = await _authRepository.GetByEmailAsync(email);
            user!.ResetToken = resetToken;

            await _authRepository.SaveChangesAsync();

            return resetToken;
        }

        public async Task ResetPasswordAsync(string resetToken, string newPassword)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(resetToken);            

            // Get email from claims
            var email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                throw new Exception("Token không hợp lệ (không có email)");

            var user = await _authRepository.GetByEmailAsync(email);
            if (user == null) throw new Exception("Người dùng không tồn tại");

            // Compare tokens in DB and request
            if (user.ResetToken != resetToken)
                throw new Exception("Token không hợp lệ (không đúng token)");

            // Get expiration time
            DateTime expires = token.ValidTo;
            if (DateTime.UtcNow > expires)
                throw new Exception("Token đã hết hạn");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetToken = "";

            await _authRepository.SaveChangesAsync();
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            var user = await _authRepository.GetByIdAsync(id);
            if (user == null) throw new Exception("Người dùng không tồn tại");
            return user;
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            var user = await _authRepository.GetByEmailAsync(email);
            return user;
        }

        public async Task ChangePasswordAsync(Account user)
        {
            var existingUser = await _authRepository.GetByIdAsync(user.Id);
            if (existingUser == null) throw new Exception("Người dùng không tồn tại");
            existingUser.PasswordHash = user.PasswordHash;
            // Cập nhật các trường khác nếu cần
            await _authRepository.ChangePasswordAsync(user);
        }

        public async Task<(string accessToken, string refreshToken)> RefreshAsync(string refreshToken, Account account)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if(storedToken == null || storedToken.AccountId != account.Id)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            if (storedToken == null || storedToken.IsRevoked)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            // ✅ Kiểm tra absolute expiration (ví dụ 90 ngày)
            int absoluteDays = int.Parse(_configuration["JwtSettings:AbsoluteExpiryDays"] ?? "90");
            if (DateTime.UtcNow > storedToken.InitialLoginAt.AddDays(absoluteDays))
                throw new UnauthorizedAccessException("Session expired, please login again.");

            // Nếu hợp lệ → cấp AccessToken + RefreshToken mới
            var newAccessToken = GenerateJwtToken(account.Email).Result; // access token mới

            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                InitialLoginAt = storedToken.InitialLoginAt, // Giữ nguyên mốc login ban đầu
                AccountId = account.Id,
                IsRevoked = false
            };

            storedToken.IsRevoked = true; // hủy token cũ
            await _refreshTokenRepository.RevokeTokenAsync(storedToken); // hủy token cũ 
            await _refreshTokenRepository.CreateTokenAsync(newRefreshToken); // lưu token mới

            return (newAccessToken, newRefreshToken.Token);
        }


        public async Task<Account> GetCurrentAccount()
        {
            // Take token from header Authorization
            var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                throw new Exception("Authorization header is missing.");
            }

            // Take Token and remove "Bearer " prefix if it exists
            var token = authorizationHeader.StartsWith("Bearer ")
                ? authorizationHeader.Substring("Bearer ".Length).Trim()
                : authorizationHeader.Trim();

            // Encode the token 
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("JWT Secret Key is not configured.");
            }

            var key = Encoding.ASCII.GetBytes(secretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    throw new Exception("Invalid token: User ID not found.");
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    throw new Exception("Invalid token: User ID is not a valid GUID.");
                }

                var account = await _authRepository.GetByIdAsync(userId);
                return account ?? throw new Exception("Account not found for the provided user ID.");
            }
            catch (SecurityTokenExpiredException)
            {
                throw new Exception("Token has expired.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to validate token: {ex.Message}", ex);
            }
        }

        public async Task<List<Account>> GetAllAccountsAsync()
        {
            return await _authRepository.GetAllAccountsAsync();
        }


    }
}

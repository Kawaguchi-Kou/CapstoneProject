using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(Account account);
        Task<bool> GetValidRegisterOtpAsync(string email, string otpCode, string purpose);
        Task<AuthResponse> LoginAsync(LoginRequest loginDto);
        Task ResendRegisterOtpAsync(string email);
        Task SendResetPasswordOtpAsync(string email);
        Task<string> GetValidResetPasswordOtpAsync(string email, string otpCode, string purpose);
        Task ResetPasswordAsync(string resetToken, string newPassword);
        Task<Account?> GetByIdAsync(Guid id);
        Task<Account?> GetByEmailAsync(string email);
        Task ChangePasswordAsync(Account user);
        Task<(string accessToken, string refreshToken)> RefreshAsync(string refreshToken, Account account);
        Task<Account> GetCurrentAccount();
        Task<List<Account>> GetAllAccountsAsync();
    }
}

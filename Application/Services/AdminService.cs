using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAuthRepository _authRepository;

        public AdminService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        // ================== Read ==================
        public async Task<List<AccountResponse>> GetAll()
        {
            var accounts = await _authRepository.GetAllAccountsAsync();
            return accounts.Select(MapToResponse).ToList();
        }

        public async Task<List<AccountResponse>> GetFilteredAccountsAsync(string? roleName, bool? isActive, string? name)
        {
            var accounts = await _authRepository.GetFilteredAccountsAsync(roleName, isActive, name);
            return accounts.Select(MapToResponse).ToList();
        }

        // ================== GET BY ID ==================
        public async Task<AccountResponse> GetById(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);

            if (account == null)
                throw new Exception("Account not found");

            return MapToResponse(account);
        }
        // ================== CREATE ==================
        public async Task<AccountResponse> CreateAccount(CreateAccountRequest request)
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                RoleId = request.RoleId,
                Name = request.Name,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.AddAsync(account);
            await _authRepository.SaveChangesAsync();

            return MapToResponse(account);
        }

        // ================== UPDATE ==================
        public async Task<AccountResponse> UpdateAccount(UpdateAccountRequest request)
        {
            var account = await _authRepository.GetByIdAsync(request.Id);

            if (account == null)
                throw new Exception("Account not found");

            account.Name = request.Name;
            account.RoleId = request.RoleId;

            await _authRepository.UpdateAsync(account);
            await _authRepository.SaveChangesAsync();

            return MapToResponse(account);
        }

        // ================== DELETE ==================
        public async Task DeleteAccount(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);

            if (account == null)
                throw new Exception("Account not found");

            await _authRepository.DeleteAsync(account);
            await _authRepository.SaveChangesAsync();
        }

        // ================== Activate/Deactivate ==================
        public async Task ActivateAccount(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);
            if (account == null) throw new Exception("Account not found");

            account.IsActive = true;
            await _authRepository.SaveChangesAsync();
        }

        public async Task DeactivateAccount(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);
            if (account == null) throw new Exception("Account not found");

            account.IsActive = false;
            await _authRepository.SaveChangesAsync();
        }
        // ================== HELPER ==================
        private AccountResponse MapToResponse(Account account)
        {
            return new AccountResponse
            {
                Id = account.Id,
                Email = account.Email,
                Name = account.Name,
                RoleName = account.Role?.Name ?? "",
                IsActive = account.IsActive
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
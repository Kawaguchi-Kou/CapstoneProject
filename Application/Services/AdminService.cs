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
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IRoleRepository _roleRepository;

        public AdminService(IAuthRepository authRepository, IRoleRepository roleRepository)
        {
            _authRepository = authRepository;
            _roleRepository = roleRepository;
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

        public async Task ImportAccountsExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            // FIX 1: đúng cách lấy roles
            var roles = await _roleRepository.GetAllAsync();

            var roleDict = roles.ToDictionary(
                r => r.Name.ToLower(),
                r => r
            );


            var existingAccounts = await _authRepository.GetAllAccountsAsync();
            var existingEmails = existingAccounts
                .Select(x => x.Email.ToLower())
                .ToHashSet();

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    string email = worksheet.Cells[row, 1].Text.Trim();
                    string password = worksheet.Cells[row, 2].Text.Trim();
                    string name = worksheet.Cells[row, 3].Text.Trim();
                    string roleRaw = worksheet.Cells[row, 4].Text.Trim().ToLower();
                    string isActiveRaw = worksheet.Cells[row, 5].Text.Trim();

                    if (string.IsNullOrWhiteSpace(email))
                        throw new Exception("Email is empty");

                    if (existingEmails.Contains(email.ToLower()))
                        throw new Exception("Email already exists");

                    if (!roleDict.TryGetValue(roleRaw, out var role))
                        throw new Exception($"Role '{roleRaw}' not found");

                    if (!bool.TryParse(isActiveRaw, out var isActive))
                        throw new Exception("Invalid IsActive");

                    var account = new Account
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        Name = name,
                        RoleId = role.Id,
                        IsActive = isActive,

                        // default
                        CreatedAt = DateTime.UtcNow,
                        Address = "",
                        PhoneNumber = "",
                        AvatarUrl = "",
                        Gender = "",
                        ResetToken = ""
                    };

                    await _authRepository.AddAsync(account);
                    existingEmails.Add(email.ToLower());
                }
                catch (Exception ex)
                {
                    throw new Exception($"Row {row}: {ex.Message}");
                }
            }

            await _authRepository.SaveChangesAsync();
        }

        public async Task<byte[]> ExportAccountsExcelAsync()
        {
            var accounts = await _authRepository.GetAllAccountsAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Accounts");

            // Header
            worksheet.Cells[1, 1].Value = "Email";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Role";
            worksheet.Cells[1, 4].Value = "IsActive";
            worksheet.Cells[1, 5].Value = "CreatedAt";

            for (int i = 0; i < accounts.Count; i++)
            {
                var acc = accounts[i];
                int row = i + 2;

                worksheet.Cells[row, 1].Value = acc.Email;
                worksheet.Cells[row, 2].Value = acc.Name;
                worksheet.Cells[row, 3].Value = acc.Role?.Name; // cần include Role
                worksheet.Cells[row, 4].Value = acc.IsActive;
                worksheet.Cells[row, 5].Value = acc.CreatedAt.ToString("yyyy-MM-dd");
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        
    }
}
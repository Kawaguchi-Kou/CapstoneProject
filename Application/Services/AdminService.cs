using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
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
                PasswordHash = HashPassword(ProvisionedAccountPassword),
                RoleId = request.RoleId,
                Name = string.IsNullOrWhiteSpace(request.Name) ? request.Email.Split('@')[0] : request.Name,
                IsActive = request.IsActive,
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow
            };
            ApplyPersistenceOnlyDefaults(account);

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

        /// <summary>Mật khẩu nội bộ cho tài khoản tạo qua API/Excel (không nằm trong 5 thuộc tính nghiệp vụ).</summary>
        private const string ProvisionedAccountPassword = "Imported@ChangeMe1";

        public async Task<byte[]> ExportAccountsExcelAsync()
        {
            var accounts = await _authRepository.GetAllAccountsAsync();
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Accounts");
            ws.Cells[1, 1].Value = "Email";
            ws.Cells[1, 2].Value = "Name";
            ws.Cells[1, 3].Value = "Role";
            ws.Cells[1, 4].Value = "IsActive";
            ws.Cells[1, 5].Value = "CreatedAt";

            int row = 2;
            foreach (var a in accounts.OrderBy(x => x.Email))
            {
                ws.Cells[row, 1].Value = a.Email;
                ws.Cells[row, 2].Value = a.Name;
                ws.Cells[row, 3].Value = a.Role?.Name ?? "";
                ws.Cells[row, 4].Value = a.IsActive;
                ws.Cells[row, 5].Value = a.CreatedAt;
                ws.Cells[row, 5].Style.Numberformat.Format = DateTimeFormatInfo.CurrentInfo.ShortDatePattern + " " + DateTimeFormatInfo.CurrentInfo.LongTimePattern;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task ImportAccountsExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];
            if (ws.Dimension == null || ws.Dimension.Rows < 2)
                return;

            var accounts = await _authRepository.GetAllAccountsAsync();
            var byEmail = accounts.ToDictionary(a => a.Email.ToLowerInvariant(), a => a);

            int rowCount = ws.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                var email = ws.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                var name = ws.Cells[row, 2].Text?.Trim() ?? "";
                var roleName = ws.Cells[row, 3].Text?.Trim();
                if (string.IsNullOrWhiteSpace(roleName))
                    throw new Exception($"Row {row}: Role is required");

                var role = await _roleRepository.GetByNameIgnoreCaseAsync(roleName);
                if (role == null)
                    throw new Exception($"Row {row}: Unknown role \"{roleName}\"");

                if (!TryParseExcelBool(ws.Cells[row, 4], out var isActive))
                    throw new Exception($"Row {row}: Invalid IsActive (use true/false, 1/0, yes/no)");

                var hasCreatedAt = TryParseExcelDateTime(ws.Cells[row, 5], out var parsedCreated);
                var createdAt = hasCreatedAt ? parsedCreated : DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(name))
                    name = email.Split('@')[0];

                var key = email.ToLowerInvariant();
                if (byEmail.TryGetValue(key, out var existing))
                {
                    existing.Name = name;
                    existing.RoleId = role.Id;
                    existing.IsActive = isActive;
                    if (hasCreatedAt)
                        existing.CreatedAt = createdAt;
                }
                else
                {
                    var account = new Account
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        PasswordHash = HashPassword(ProvisionedAccountPassword),
                        Name = name,
                        RoleId = role.Id,
                        IsActive = isActive,
                        CreatedAt = createdAt
                    };
                    ApplyPersistenceOnlyDefaults(account);
                    await _authRepository.AddAsync(account);
                    byEmail[key] = account;
                }
            }

            await _authRepository.SaveChangesAsync();
        }

        private static bool TryParseExcelBool(ExcelRange cell, out bool value)
        {
            if (cell.Value is bool b)
            {
                value = b;
                return true;
            }

            var t = cell.Text?.Trim();
            if (string.IsNullOrEmpty(t))
            {
                value = false;
                return false;
            }

            if (bool.TryParse(t, out value))
                return true;

            if (t == "1" || t.Equals("yes", StringComparison.OrdinalIgnoreCase) || t.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (t == "0" || t.Equals("no", StringComparison.OrdinalIgnoreCase) || t.Equals("inactive", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static bool TryParseExcelDateTime(ExcelRange cell, out DateTime value)
        {
            if (cell.Value is DateTime dt)
            {
                value = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                return true;
            }

            if (cell.Value is double oa)
            {
                try
                {
                    value = DateTime.SpecifyKind(DateTime.FromOADate(oa), DateTimeKind.Utc);
                    return true;
                }
                catch
                {
                    // fall through
                }
            }

            if (DateTime.TryParse(cell.Text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value))
                return true;

            return false;
        }

        // ================== HELPER ==================
        /// <summary>Giá trị bắt buộc của DB/EF, không thuộc 5 thuộc tính import/export.</summary>
        private static void ApplyPersistenceOnlyDefaults(Account account)
        {
            account.DateOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            account.Gender = "Unknown";
            account.Address = string.Empty;
            account.PhoneNumber = string.Empty;
            account.AvatarUrl = string.Empty;
            account.ResetToken = string.Empty;
        }

        private AccountResponse MapToResponse(Account account)
        {
            return new AccountResponse
            {
                Id = account.Id,
                Email = account.Email,
                Name = account.Name,
                RoleName = account.Role?.Name ?? "",
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt
            };
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
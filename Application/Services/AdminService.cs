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
using System.Text.RegularExpressions;
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
            if (string.IsNullOrWhiteSpace(request.Email) || !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Invalid email format");

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
                throw new ArgumentException("Name must be at least 2 characters");

            var accounts = await _authRepository.GetAllAccountsAsync();
            if (accounts.Any(a => a.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Email already exists");

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
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
                throw new ArgumentException("Name must be at least 2 characters");

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
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Normalize header text: lowercase + keep only [a-z0-9] so "Is Active", "IsActive", etc map consistently.
            string NormalizeHeader(string s)
            {
                var normalized = (s ?? "").Trim().ToLowerInvariant();
                normalized = Regex.Replace(normalized, @"[^a-z0-9]", "");
                return normalized;
            }

            // Some files may have a blank row above the header; scan the first few rows to find a valid header.
            Dictionary<string, int> headerMap = new Dictionary<string, int>();
            int headerRow = -1;
            for (int candidateRow = 1; candidateRow <= Math.Min(5, rowCount); candidateRow++)
            {
                var temp = new Dictionary<string, int>();
                for (int col = 1; col <= colCount; col++)
                {
                    var header = NormalizeHeader(worksheet.Cells[candidateRow, col].Text);
                    if (!string.IsNullOrWhiteSpace(header) && !temp.ContainsKey(header))
                    {
                        temp[header] = col;
                    }
                }

                // We must at least find Role + IsActive (and usually Email).
                if (temp.ContainsKey("role") && temp.ContainsKey("isactive"))
                {
                    headerMap = temp;
                    headerRow = candidateRow;
                    break;
                }
            }

            if (headerRow == -1)
            {
                // Fall back to row 1 if we couldn't find a header; defaults match your exported layout:
                // Email (1), Name (2), Role (3), IsActive (4), CreatedAt (5)
                headerRow = 1;
                for (int col = 1; col <= colCount; col++)
                {
                    var header = NormalizeHeader(worksheet.Cells[headerRow, col].Text);
                    if (!string.IsNullOrWhiteSpace(header) && !headerMap.ContainsKey(header))
                    {
                        headerMap[header] = col;
                    }
                }
            }

            int emailCol = headerMap.TryGetValue("email", out var tmpEmailCol) ? tmpEmailCol : 1;
            int nameCol = headerMap.TryGetValue("name", out var tmpNameCol) ? tmpNameCol : 2;
            int roleCol = headerMap.TryGetValue("role", out var tmpRoleCol) ? tmpRoleCol : 3;
            int isActiveCol = headerMap.TryGetValue("isactive", out var tmpIsActiveCol) ? tmpIsActiveCol : 4;
            int passwordCol = headerMap.TryGetValue("password", out var tmpPasswordCol) ? tmpPasswordCol : -1;

            // Bỏ lỗi ném ra nếu không thấy header chứa role và isactive, fallback về column 3 và 4 mặc định

            // FIX 1: đúng cách lấy roles
            var roles = await _roleRepository.GetAllAsync();

            var roleDict = roles.ToDictionary(
                r => r.Name.ToLower(),
                r => r
            );


            var existingAccounts = await _authRepository.GetAllAccountsAsync();
            var existingDict = existingAccounts.ToDictionary(a => a.Email.ToLower());

            for (int row = headerRow + 1; row <= rowCount; row++)
            {
                try
                {
                    string email = worksheet.Cells[row, emailCol].Text.Trim();
                    string name = worksheet.Cells[row, nameCol].Text.Trim();
                    string roleRaw = worksheet.Cells[row, roleCol].Text.Trim().ToLower();
                    string isActiveRaw = worksheet.Cells[row, isActiveCol].Text.Trim();
                    string passwordRaw = passwordCol > 0 ? worksheet.Cells[row, passwordCol].Text.Trim() : "";

                    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(name))
                        continue;

                    if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        continue;

                    if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
                    {
                        var parts = email.Split('@');
                        name = parts.Length > 0 ? parts[0] : "User";
                    }

                    if (!roleDict.TryGetValue(roleRaw, out var role))
                    {
                        if (roleDict.TryGetValue("user", out var defaultRole))
                            role = defaultRole;
                        else
                            continue; // Bỏ qua nếu không có role hợp lệ
                    }

                    if (string.IsNullOrWhiteSpace(isActiveRaw) || isActiveRaw == "1" || isActiveRaw.ToLower() == "active" || isActiveRaw.ToLower() == "yes" || isActiveRaw.ToLower() == "true") 
                        isActiveRaw = "true";
                    else if (isActiveRaw == "0" || isActiveRaw.ToLower() == "inactive" || isActiveRaw.ToLower() == "no" || isActiveRaw.ToLower() == "false") 
                        isActiveRaw = "false";

                    if (!bool.TryParse(isActiveRaw, out var isActive))
                        isActive = true;

                    if (existingDict.TryGetValue(email.ToLower(), out var existingAccount))
                    {
                        // Update
                        existingAccount.Name = name;
                        existingAccount.RoleId = role.Id;
                        existingAccount.IsActive = isActive;
                        
                        if (!string.IsNullOrWhiteSpace(passwordRaw))
                        {
                            existingAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordRaw);
                        }
                        
                        await _authRepository.UpdateAsync(existingAccount);
                    }
                    else
                    {
                        // Create
                        string password = string.IsNullOrWhiteSpace(passwordRaw) ? "123456" : passwordRaw;
                        var account = new Account
                        {
                            Id = Guid.NewGuid(),
                            Email = email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                            Name = name,
                            RoleId = role.Id,
                            IsActive = isActive,

                            CreatedAt = DateTime.UtcNow,
                            Address = "",
                            PhoneNumber = "",
                            AvatarUrl = "",
                            Gender = "",
                            ResetToken = ""
                        };

                        await _authRepository.AddAsync(account);
                        existingDict[email.ToLower()] = account;
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi 1 dòng để tiếp tục import các dòng khác
                    continue;
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
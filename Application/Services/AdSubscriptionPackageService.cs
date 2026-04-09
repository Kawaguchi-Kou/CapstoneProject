using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Application.Services
{
    public class AdSubscriptionPackageService : IAdSubscriptionPackageService
    {
        private readonly IAdSubscriptionPackageRepository _packageRepository;

        // Swagger UI thường điền placeholder "string". Nếu FE/Swagger gửi nguyên "string"
        // thì ta coi như chưa nhập và dùng default hợp lệ.
        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "active",
                "inactive"
            };

        public AdSubscriptionPackageService(IAdSubscriptionPackageRepository packageRepository)
        {
            _packageRepository = packageRepository;
        }

        private static string NormalizeStatusForCreate(string? status)
        {
            var raw = status?.Trim();

            if (string.IsNullOrWhiteSpace(raw) ||
                string.Equals(raw, "string", StringComparison.OrdinalIgnoreCase))
            {
                return "active";
            }

            var normalized = raw.ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalized))
            {
                throw new ArgumentException(
                    $"Status không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedStatuses)}");
            }

            return normalized;
        }

        private static string? NormalizeStatusForUpdate(string? status)
        {
            var raw = status?.Trim();

            if (string.IsNullOrWhiteSpace(raw) ||
                string.Equals(raw, "string", StringComparison.OrdinalIgnoreCase))
            {
                return null; // không update
            }

            var normalized = raw.ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalized))
            {
                throw new ArgumentException(
                    $"Status không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedStatuses)}");
            }

            return normalized;
        }

        public async Task<AdSubscriptionPackage> CreatePackageAsync(CreateAdSubscriptionPackageRequest request)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length < 2)
                throw new ArgumentException("Title must be at least 2 characters");

            if (request.Price < 0)
                throw new ArgumentException("Price cannot be negative");

            if (request.DurationDays <= 0)
                throw new ArgumentException("DurationDays must be greater than 0");

            if (request.MaxAdsPerPeriod <= 0)
                throw new ArgumentException("MaxAdsPerPeriod must be greater than 0");

            // Create package entity
            var package = new AdSubscriptionPackage
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                Price = request.Price,
                DurationDays = request.DurationDays,
                MaxAdsPerPeriod = request.MaxAdsPerPeriod,
                Status = NormalizeStatusForCreate(request.Status),
                Currency = request.Currency ?? "VND"
            };

            return await _packageRepository.CreateAsync(package);
        }

        public async Task<AdSubscriptionPackage?> GetPackageByIdAsync(Guid packageId)
        {
            return await _packageRepository.GetByIdAsync(packageId);
        }

        public async Task<List<AdSubscriptionPackage>> GetAllPackagesAsync()
        {
            return await _packageRepository.GetAllAsync();
        }
        
        public async Task<List<AdSubscriptionPackage>> GetFilteredPackagesAsync(string? title, string? status, string? sortPrice)
        {
            var packages = await _packageRepository.GetAllAsync();
            var query = packages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(p => p.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Status", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(sortPrice))
            {
                if (sortPrice.Equals("asc", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderBy(p => p.Price);
                }
                else if (sortPrice.Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }

            return query.ToList();
        }

        public async Task<AdSubscriptionPackage> UpdatePackageAsync(Guid packageId, CreateAdSubscriptionPackageRequest request)
        {
            var existingPackage = await _packageRepository.GetByIdAsync(packageId);
            if (existingPackage == null)
                throw new KeyNotFoundException("Package not found");

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length < 2)
                throw new ArgumentException("Title must be at least 2 characters");

            if (request.Price < 0)
                throw new ArgumentException("Price cannot be negative");

            if (request.DurationDays <= 0)
                throw new ArgumentException("DurationDays must be greater than 0");

            if (request.MaxAdsPerPeriod <= 0)
                throw new ArgumentException("MaxAdsPerPeriod must be greater than 0");

            // Update package
            existingPackage.Title = request.Title;
            existingPackage.Description = request.Description ?? string.Empty;
            existingPackage.Price = request.Price;
            existingPackage.DurationDays = request.DurationDays;
            existingPackage.MaxAdsPerPeriod = request.MaxAdsPerPeriod;

            var normalizedStatus = NormalizeStatusForUpdate(request.Status);
            if (normalizedStatus != null)
            {
                existingPackage.Status = normalizedStatus;
            }

            existingPackage.Currency = request.Currency ?? existingPackage.Currency;

            return await _packageRepository.UpdateAsync(existingPackage);
        }

        public async Task<bool> DeletePackageAsync(Guid packageId)
        {
            return await _packageRepository.DeleteAsync(packageId);
        }

        public async Task ActivatePackageAsync(Guid packageId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);

            if (package == null)
                throw new KeyNotFoundException("Package not found");

            package.Status = "active";

            await _packageRepository.UpdateAsync(package);
        }

        public async Task DeactivatePackageAsync(Guid packageId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);

            if (package == null)
                throw new KeyNotFoundException("Package not found");

            package.Status = "inactive";

            await _packageRepository.UpdateAsync(package);
        }

        public async Task ImportPackagesExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            var existingPackages = await _packageRepository.GetAllAsync();
            var existingTitles = existingPackages.Select(p => p.Title.ToLower()).ToHashSet();

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    string title = worksheet.Cells[row, 1].Text.Trim();
                    string desc = worksheet.Cells[row, 2].Text.Trim();
                    decimal price = decimal.TryParse(worksheet.Cells[row, 3].Text.Trim(), out var p) ? p : 0;
                    int duration = int.TryParse(worksheet.Cells[row, 4].Text.Trim(), out var d) ? d : 0;
                    int maxAds = int.TryParse(worksheet.Cells[row, 5].Text.Trim(), out var m) ? m : 0;
                    string statusRaw = worksheet.Cells[row, 6].Text.Trim().ToLower();
                    string currency = string.IsNullOrWhiteSpace(worksheet.Cells[row, 7].Text.Trim()) ? "VND" : worksheet.Cells[row, 7].Text.Trim();

                    if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc))
                        continue;

                    if (string.IsNullOrWhiteSpace(title) || title.Length < 2)
                        throw new Exception("Package title must be at least 2 characters");

                    if (existingTitles.Contains(title.ToLower()))
                    {
                        continue; // Bỏ qua thay vì ném ra Exception
                    }

                    if (!AllowedStatuses.Contains(statusRaw))
                        statusRaw = "active";

                    var packageEntity = new AdSubscriptionPackage
                    {
                        PackageId = Guid.NewGuid(),
                        Title = title,
                        Description = desc,
                        Price = price,
                        DurationDays = duration,
                        MaxAdsPerPeriod = maxAds,
                        Status = statusRaw,
                        Currency = currency,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _packageRepository.CreateAsync(packageEntity);
                    existingTitles.Add(title.ToLower());
                }
                catch (Exception ex)
                {
                    throw new Exception($"Row {row}: {ex.Message}");
                }
            }

            await _packageRepository.SaveChangesAsync();
        }

        public async Task<byte[]> ExportPackagesExcelAsync()
        {
            var packages = await _packageRepository.GetAllAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("AdSubscriptionPackages");

            // Header
            worksheet.Cells[1, 1].Value = "Title";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Price";
            worksheet.Cells[1, 4].Value = "DurationDays";
            worksheet.Cells[1, 5].Value = "MaxAdsPerPeriod";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "Currency";
            worksheet.Cells[1, 8].Value = "CreatedAt";

            for (int i = 0; i < packages.Count; i++)
            {
                var p = packages[i];
                int row = i + 2;

                worksheet.Cells[row, 1].Value = p.Title;
                worksheet.Cells[row, 2].Value = p.Description;
                worksheet.Cells[row, 3].Value = p.Price;
                worksheet.Cells[row, 4].Value = p.DurationDays;
                worksheet.Cells[row, 5].Value = p.MaxAdsPerPeriod;
                worksheet.Cells[row, 6].Value = p.Status;
                worksheet.Cells[row, 7].Value = p.Currency;
                worksheet.Cells[row, 8].Value = p.CreatedAt.ToString("yyyy-MM-dd");
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }
    }
}


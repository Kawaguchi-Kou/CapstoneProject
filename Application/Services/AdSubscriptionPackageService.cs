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
            int colCount = worksheet.Dimension.Columns;

            // Normalize header text
            string NormalizeHeader(string s)
            {
                var normalized = (s ?? "").Trim().ToLowerInvariant();
                normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]", "");
                return normalized;
            }

            // Scan headers
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
                if (temp.ContainsKey("title") || temp.ContainsKey("price"))
                {
                    headerMap = temp;
                    headerRow = candidateRow;
                    break;
                }
            }

            if (headerRow == -1)
            {
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

            int titleCol = headerMap.TryGetValue("title", out var tmpTitleCol) ? tmpTitleCol : 1;
            int descCol = headerMap.TryGetValue("description", out var tmpDescCol) ? tmpDescCol : 2;
            int priceCol = headerMap.TryGetValue("price", out var tmpPriceCol) ? tmpPriceCol : 3;
            int durationCol = headerMap.TryGetValue("durationdays", out var tmpDurationCol) ? tmpDurationCol : (headerMap.TryGetValue("duration", out var t1) ? t1 : 4);
            int maxAdsCol = headerMap.TryGetValue("maxadsperperiod", out var tmpMaxAdsCol) ? tmpMaxAdsCol : (headerMap.TryGetValue("maxads", out var t2) ? t2 : 5);
            int statusCol = headerMap.TryGetValue("status", out var tmpStatusCol) ? tmpStatusCol : 6;
            int currencyCol = headerMap.TryGetValue("currency", out var tmpCurrencyCol) ? tmpCurrencyCol : 7;

            var existingPackages = await _packageRepository.GetAllAsync();
            var existingDict = existingPackages.ToDictionary(p => p.Title.ToLower());

            for (int row = headerRow + 1; row <= rowCount; row++)
            {
                try
                {
                    string title = worksheet.Cells[row, titleCol].Text?.Trim() ?? "";
                    string desc = worksheet.Cells[row, descCol].Text?.Trim() ?? "";
                    decimal price = decimal.TryParse(worksheet.Cells[row, priceCol].Text?.Trim(), out var p) ? p : 0;
                    int duration = int.TryParse(worksheet.Cells[row, durationCol].Text?.Trim(), out var d) ? d : 30; // default 30 days
                    int maxAds = int.TryParse(worksheet.Cells[row, maxAdsCol].Text?.Trim(), out var m) ? m : 5; // default 5 ads
                    string statusRaw = worksheet.Cells[row, statusCol].Text?.Trim()?.ToLower() ?? "";
                    string currency = string.IsNullOrWhiteSpace(worksheet.Cells[row, currencyCol].Text?.Trim()) ? "VND" : worksheet.Cells[row, currencyCol].Text.Trim();

                    if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc))
                        continue;

                    if (string.IsNullOrWhiteSpace(title) || title.Length < 2)
                    {
                        title = "Package " + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + row;
                    }

                    if (!AllowedStatuses.Contains(statusRaw))
                        statusRaw = "active";

                    if (existingDict.TryGetValue(title.ToLower(), out var existingPackage))
                    {
                        // Update
                        existingPackage.Description = desc;
                        existingPackage.Price = price;
                        existingPackage.DurationDays = duration;
                        existingPackage.MaxAdsPerPeriod = maxAds;
                        existingPackage.Status = statusRaw;
                        existingPackage.Currency = currency;
                        
                        await _packageRepository.UpdateAsync(existingPackage);
                    }
                    else
                    {
                        // Create
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
                        existingDict[title.ToLower()] = packageEntity;
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua dòng lỗi để import các dòng khác
                    continue;
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


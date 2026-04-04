using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

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
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required");

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
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required");

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

        public async Task<List<AdSubscriptionPackage>> ImportPackagesFromCsvAsync(byte[] fileBytes)
        {
            var csvContent = Encoding.UTF8.GetString(fileBytes);
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<AdSubscriptionPackage>();

            for (int i = 1; i < lines.Length; i++) // Bỏ header
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 8) continue; // đảm bảo đủ cột

                var package = new AdSubscriptionPackage
                {
                    PackageId = Guid.NewGuid(),
                    Title = cols[1].Trim(),
                    Description = cols[2].Trim(),
                    Price = decimal.TryParse(cols[3], out var price) ? price : 0,
                    DurationDays = int.TryParse(cols[4], out var duration) ? duration : 0,
                    MaxAdsPerPeriod = int.TryParse(cols[5], out var maxAds) ? maxAds : 0,
                    Status = AllowedStatuses.Contains(cols[6].Trim(), StringComparer.OrdinalIgnoreCase) ? cols[6].Trim().ToLowerInvariant() : "active",
                    Currency = string.IsNullOrWhiteSpace(cols[7]) ? "VND" : cols[7].Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                result.Add(package);
            }

            foreach (var p in result)
            {
                await _packageRepository.CreateAsync(p);
            }

            return result;
        }

        public async Task<byte[]> ExportPackagesToCsvAsync()
        {
            var packages = await _packageRepository.GetAllAsync();

            var csvBuilder = new StringBuilder();
            // Header
            csvBuilder.AppendLine("PackageId,Title,Description,Price,DurationDays,MaxAdsPerPeriod,Status,Currency,CreatedAt");

            foreach (var p in packages)
            {
                csvBuilder.AppendLine($"{p.PackageId},{p.Title},{p.Description},{p.Price},{p.DurationDays},{p.MaxAdsPerPeriod},{p.Status},{p.Currency},{p.CreatedAt:O}");
            }

            return Encoding.UTF8.GetBytes(csvBuilder.ToString());
        }
    }
}

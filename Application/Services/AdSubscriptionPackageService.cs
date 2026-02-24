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
    }
}

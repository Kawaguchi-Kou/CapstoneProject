using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IAdSubscriptionPackageService
    {
        Task<AdSubscriptionPackage> CreatePackageAsync(CreateAdSubscriptionPackageRequest request);
        Task<AdSubscriptionPackage?> GetPackageByIdAsync(Guid packageId);
        Task<List<AdSubscriptionPackage>> GetAllPackagesAsync();
        Task<List<AdSubscriptionPackage>> GetFilteredPackagesAsync(string? title, string? status, string? sortPrice);
        Task<AdSubscriptionPackage> UpdatePackageAsync(Guid packageId, CreateAdSubscriptionPackageRequest request);
        Task<bool> DeletePackageAsync(Guid packageId);
        Task ActivatePackageAsync(Guid packageId);
        Task DeactivatePackageAsync(Guid packageId);

        Task<List<AdSubscriptionPackage>> ImportPackagesFromCsvAsync(byte[] fileBytes);
        Task<byte[]> ExportPackagesToCsvAsync();
        Task ImportPackagesFromCsvAsync(IFormFile file);
    }
}

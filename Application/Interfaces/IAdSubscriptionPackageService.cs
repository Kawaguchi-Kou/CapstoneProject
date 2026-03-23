using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAdSubscriptionPackageService
    {
        Task<AdSubscriptionPackage> CreatePackageAsync(CreateAdSubscriptionPackageRequest request);
        Task<AdSubscriptionPackage?> GetPackageByIdAsync(Guid packageId);
        Task<List<AdSubscriptionPackage>> GetAllPackagesAsync();
        Task<AdSubscriptionPackage> UpdatePackageAsync(Guid packageId, CreateAdSubscriptionPackageRequest request);
        Task<bool> DeletePackageAsync(Guid packageId);
        Task ActivatePackageAsync(Guid packageId);
        Task DeactivatePackageAsync(Guid packageId);
    }
}

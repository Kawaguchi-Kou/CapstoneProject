using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAdSubscriptionPackageRepository
    {
        Task<AdSubscriptionPackage?> GetByIdAsync(Guid packageId);
        Task<List<AdSubscriptionPackage>> GetAllAsync();
        Task<AdSubscriptionPackage> CreateAsync(AdSubscriptionPackage package);
        Task<AdSubscriptionPackage> UpdateAsync(AdSubscriptionPackage package);
        Task<bool> DeleteAsync(Guid packageId);
        Task SaveChangesAsync();
    }
}

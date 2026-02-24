using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IAdvertisementRepository
    {
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId);
        Task<Advertisement> CreateAsync(Advertisement advertisement);
        Task<Advertisement> UpdateAsync(Advertisement advertisement);
        Task SaveChangesAsync();
    }
}

using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAdvertisementService
    {
        Task<Advertisement> CreateAdvertisementAsync(Guid accountId, CreateAdvertisementRequest request);
        Task<Advertisement?> GetByIdAsync(Guid adId);
        Task<List<Advertisement>> GetByAccountIdAsync(Guid accountId);
        Task<Advertisement> ApproveAdvertisementAsync(Guid adId);
        Task<Advertisement> RejectAdvertisementAsync(Guid adId, string? reason = null);
    }
}

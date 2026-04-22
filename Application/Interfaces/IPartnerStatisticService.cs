using Application.DTOs.Responses;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPartnerStatisticService
    {
        Task<PartnerDashboardResponse> GetDashboardStatsAsync(Guid partnerId);
    }
}

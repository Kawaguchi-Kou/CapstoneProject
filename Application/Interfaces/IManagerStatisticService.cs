using Application.DTOs.Responses;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IManagerStatisticService
    {
        Task<ManagerDashboardResponse> GetManagerDashboardStatisticsAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null);
    }
}

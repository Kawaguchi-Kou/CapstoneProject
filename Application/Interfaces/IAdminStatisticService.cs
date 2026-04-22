using System.Threading.Tasks;
using Application.DTOs.Responses;

namespace Application.Interfaces
{
    public interface IAdminStatisticService
    {
        Task<AdminDashboardResponse> GetDashboardStatisticsAsync(string period = "daily", DateTime? startDate = null, DateTime? endDate = null);
    }
}

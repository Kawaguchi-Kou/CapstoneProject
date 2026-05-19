using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs
{
    public interface IAdSchedulingJob
    {
        Task ProcessScheduledAndExpiredAdsAsync();
    }
}

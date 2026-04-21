using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs
{
    public interface IPaymentExpiryJob
    {
        Task ExpirePendingPaymentsAsync();
    }
}

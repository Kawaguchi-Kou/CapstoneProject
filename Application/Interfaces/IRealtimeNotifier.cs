using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task SendGroupNotificationAsync(Guid scheduleId, object notification);
        Task SendUserNotificationAsync(Guid userId, object notification);
        Task SendBroadcastNotificationAsync(object notification);
    }
}

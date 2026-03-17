using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface INotificationRecipientService
    {
        Task UpdateNoticationRecipientAsync(Guid notificationRecipientId);
        Task<List<NotificationRecipient>?> GetAllNotificationRecipientsByUserIdAsync(Guid userId);
    }
}

using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface INotificationRecipientRepository
    {
        Task SaveChangesAsync();
        Task CreateNotificationRecipientsAsync(List<NotificationRecipient> notificationRecipients);
        Task<NotificationRecipient?> GetNotificationRecipientByIdAsync(Guid notificationRecipientId);
        Task<List<NotificationRecipient>?> GetAllNotificationRecipientsByUserIdAsync(Guid userId);
    }
}

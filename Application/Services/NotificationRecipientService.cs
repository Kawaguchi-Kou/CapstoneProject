using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class NotificationRecipientService : INotificationRecipientService
    {
        private readonly INotificationRecipientRepository _notificationRecipientRepository;

        public NotificationRecipientService(INotificationRecipientRepository notificationRecipientRepository)
        {
            _notificationRecipientRepository = notificationRecipientRepository;
        }

        public async Task UpdateNoticationRecipientAsync(Guid notificationRecipientId)
        {
            var recipient = await _notificationRecipientRepository.GetNotificationRecipientByIdAsync(notificationRecipientId);
            if (recipient != null && recipient.IsRead == false)
            {
                recipient.IsRead = true;
                recipient.ReadAt = DateTime.UtcNow;
                await _notificationRecipientRepository.SaveChangesAsync();
            }
            else
            {
                throw new Exception("No notification recipient was found or this notifiaction was read");
            }
        }

        public async Task<List<NotificationRecipient>?> GetAllNotificationRecipientsByUserIdAsync(Guid userId)
        {
            var notificationRecipients = await _notificationRecipientRepository.GetAllNotificationRecipientsByUserIdAsync(userId);

            return notificationRecipients;
        }
    }
}

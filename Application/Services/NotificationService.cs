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
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRecipientRepository _notificationRecipientRepository;
 //       private readonly IScheduleParticipantRepository _participantRepository;

        public NotificationService(INotificationRepository notificationRepository, INotificationRecipientRepository notificationRecipientRepository
//            , IScheduleParticipantRepository scheduleParticipantRepository
            )
        {
            _notificationRepository = notificationRepository;
            _notificationRecipientRepository = notificationRecipientRepository;
  //          _participantRepository = scheduleParticipantRepository;
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            await _notificationRepository.AddAsync(notification);

            if (notification.TripId != null)
            {
                var participants = new List<Participant>();
                //await _participantRepository.GetAllParticipantByScheduleIdAsync(notification.TripId);
                var recipients = new List<NotificationRecipient>();
                foreach (var participant in participants)
                {
                    // Except sender of notification
                    if (participant.UserId != notification.SenderId)
                    {
                        recipients.Add(new NotificationRecipient
                        {
                            Id = Guid.NewGuid(),
                            NotificationId = notification.Id,
                            RecipientId = participant.UserId,
                            IsRead = false,
                            ReadAt = null
                        });
                    }
                }

                await _notificationRecipientRepository.CreateNotificationRecipientsAsync(recipients);
            }
            else if (notification.RecipientId != null)
            {
                var recipient = new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    RecipientId = notification.RecipientId,
                    IsRead = false,
                    ReadAt = DateTime.UtcNow
                };
                await _notificationRecipientRepository.CreateNotificationRecipientsAsync(new List<NotificationRecipient> { recipient });
            }
        }
    }
}

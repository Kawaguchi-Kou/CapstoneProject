using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRecipientRepository _recipientRepository;
        private readonly IParticipantRepository _participantRepository;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationRecipientRepository recipientRepository,
            IParticipantRepository participantRepository)
        {
            _notificationRepository = notificationRepository;
            _recipientRepository = recipientRepository;
            _participantRepository = participantRepository;
        }

        public async Task CreateNotificationAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TripId = request.TripId,
                SenderId = request.SenderId,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);

            var recipients = new List<NotificationRecipient>();

            // ⭐ Case 1: gửi theo Trip
                var participants = await _participantRepository
                    .GetAllParticipantByTripIdAsync(request.TripId);

                foreach (var p in participants)
                {
                    if (p.UserId == request.SenderId) continue;

                    recipients.Add(new NotificationRecipient
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = notification.Id,
                        RecipientId = p.UserId,
                        IsRead = false,
                        ReadAt = null
                    });
                }
            

            // ⭐ Case 2: gửi 1 user

                recipients.Add(new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    RecipientId = request.RecipientId,
                    IsRead = false,
                    ReadAt = null
                });
            

            await _recipientRepository
                .CreateNotificationRecipientsAsync(recipients);
        }
    }
}

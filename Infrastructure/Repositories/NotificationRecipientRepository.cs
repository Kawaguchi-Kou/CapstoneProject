using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.EntitiesConfigurations;

namespace Infrastructure.Repositories
{
    public class NotificationRecipientRepository : INotificationRecipientRepository
    {
        private readonly AppDbContext _context;

        public NotificationRecipientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationRecipientsAsync(List<NotificationRecipient> notificationRecipients)
        {
            await _context.Recipients.AddRangeAsync(notificationRecipients);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<NotificationRecipient?> GetNotificationRecipientByIdAsync(Guid notificationRecipientId)
        {
            return await Task.FromResult(_context.Recipients
                .FirstOrDefault(nr => nr.Id == notificationRecipientId));
        }

        public async Task<List<NotificationRecipient>?> GetAllNotificationRecipientsByUserIdAsync(Guid userId)
        {
            return await Task.FromResult(_context.Recipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.RecipientId == userId).ToList());
        }
    }
}

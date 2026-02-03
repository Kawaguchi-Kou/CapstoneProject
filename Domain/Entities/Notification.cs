using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid SenderId { get; set; }
        [Required]
        public Guid RecipientId { get; set; }
        [Required]
        public Guid TripId { get; set; }

        [ForeignKey("SenderId")]
        public Account? Sender { get; set; }
        [ForeignKey("RecipientId")]
        public ICollection<NotificationRecipient> Recipients { get; set; }
        [ForeignKey("TripId")]
        public Trip? Trip { get; set; }
    }
}

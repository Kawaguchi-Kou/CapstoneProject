using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class NotificationRecipient
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid NotificationId { get; set; }
        public Guid RecipientId { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; } = false;
        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }
        [ForeignKey("RecipientId")]
        public Account? Recipient { get; set; }
    }
}

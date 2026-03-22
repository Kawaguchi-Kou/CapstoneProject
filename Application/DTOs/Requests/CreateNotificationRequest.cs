using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class CreateNotificationRequest
    {
        public Guid TripId { get; set; }

        public Guid RecipientId { get; set; }

        public Guid SenderId { get; set; }

        public string Message { get; set; } = default!;
    }
}

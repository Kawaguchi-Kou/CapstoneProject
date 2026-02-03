using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Feedback
    {
        public Guid FeedbackId { get; set; }

        public Guid UserId { get; set; }
        public Guid? AdId { get; set; }

        public string FeedbackType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Account User { get; set; } = null!;
        public Advertisement? Advertisement { get; set; }
    }

}

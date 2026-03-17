using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class Participant
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
        public ParticipantStatus Status { get; set; }
        public ParticipantRole Role { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trip? Trip { get; set; }
        [ForeignKey(nameof(UserId))]
        public Account? User { get; set; }
    }
}

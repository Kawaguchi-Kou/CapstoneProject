using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class TripResponse
    {
        public Guid TripId { get; set; }
        public Guid OwnerId { get; set; }
        public string Title { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public TripStatus Status { get; set; }

        public TripType TripType { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

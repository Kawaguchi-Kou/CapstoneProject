using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs.Requests
{
    public class TripRequest
    {
        public string Title { get; set; }

        public string StartLocation { get; set; } = string.Empty;
        public string EndLocation { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}

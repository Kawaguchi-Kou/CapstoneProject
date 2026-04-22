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

        public string StartLocation { get; set; }
        public Guid StartDistrictId { get; set; } // 🔥 ADD

        public string EndLocation { get; set; }
        public Guid EndDistrictId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}

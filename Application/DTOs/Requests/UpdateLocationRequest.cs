using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class UpdateLocationRequest
    {
        public string? LocationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class LocationDistrictKey
    {
        public Guid LocationId { get; set; }
        public Guid DistrictId { get; set; }
    }

}

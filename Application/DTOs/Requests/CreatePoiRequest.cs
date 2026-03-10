using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class CreatePoiRequest
    {
        public string Name { get; set; }

        public string City { get; set; }
        public string Address { get; set; }

        public string Description { get; set; }
    }
}

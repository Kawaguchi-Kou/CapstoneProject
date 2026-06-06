using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class POIItemResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? POIImgUrl { get; set; }

        public POIType Type { get; set; }

        public bool IsIndoor { get; set; }
    }
}

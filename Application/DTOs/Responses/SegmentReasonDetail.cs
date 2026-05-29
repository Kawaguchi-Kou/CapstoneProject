using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class SegmentReasonDetail
    {
        public SegmentReason Reason { get; set; }
        public string MessageKey { get; set; } // optional
        public Dictionary<string, object> Metadata { get; set; }
    }
}

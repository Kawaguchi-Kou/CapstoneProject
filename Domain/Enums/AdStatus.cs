using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum AdStatus
    {
        Draft,
        PendingApproval,
        Active,
        Paused,
        Expired,
        Rejected
    }
}

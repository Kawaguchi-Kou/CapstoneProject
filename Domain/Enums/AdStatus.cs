using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum AdStatus
    {
        PendingApproval = 0,
        Active = 1,
        Paused = 2,
        Expired = 3,
        Rejected = 4,
        Scheduled = 5
    }
}

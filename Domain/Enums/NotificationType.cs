using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum NotificationType
    {
        ScheduleJoined,
        ScheduleKicked,
        ScheduleLeft,
        ScheduleUpdated,
        ScheduleDeleted,
        ScheduleRestored,
        ScheduleRoleChanged,
        ScheduleBanned,
        ActivityCreated,
        ActivityUpdated,
        ActivityDeleted,
        ActivityRestored,
        OwnerAnnouncement
    }
}

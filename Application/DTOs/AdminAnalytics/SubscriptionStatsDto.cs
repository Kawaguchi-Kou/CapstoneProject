using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.AdminAnalytics
{
    public class SubscriptionStatsDto
    {
        public int TotalPackages { get; set; }
        public int ActivePackages { get; set; }
        public int InactivePackages { get; set; }
    }
}

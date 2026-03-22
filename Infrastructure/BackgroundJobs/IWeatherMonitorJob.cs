using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs
{
    public interface IWeatherMonitorJob
    {
        Task ScanUpcomingTripsAsync();
    }
}

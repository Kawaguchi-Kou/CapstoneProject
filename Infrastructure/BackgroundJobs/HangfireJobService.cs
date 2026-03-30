using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Hangfire;

namespace Infrastructure.BackgroundJobs
{
    public class HangfireJobService : IBackgroundJobService
    {
        public void EnqueueWeatherPreload(Guid tripId)
        {
            BackgroundJob.Enqueue<IWeatherPreloadJob>(
                x => x.PreloadTripWeather(tripId)
            );
        }
    }
}

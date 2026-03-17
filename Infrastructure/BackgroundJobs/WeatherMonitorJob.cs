using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Interfaces;

namespace Infrastructure.BackgroundJobs
{
    public class WeatherMonitorJob
    {
        private readonly ITripRepository _tripRepository;
        private readonly IWeatherRiskScanService _riskScan;
        private readonly INotificationService _noti;

        public WeatherMonitorJob(
            ITripRepository tripRepository,
            IWeatherRiskScanService riskScan,
            INotificationService noti)
        {
            _tripRepository = tripRepository;
            _riskScan = riskScan;
            _noti = noti;
        }

        public async Task ScanUpcomingTripsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var trips = await _tripRepository
                .GetUpcomingTripsAsync(today);

            //foreach (var trip in trips)
            //{
            //    var summary = await _riskScan.ScanAsync(trip.TripId);

            //    if (summary.HasHighRisk)
            //    {
            //        await _noti.SendAsync(
            //            summary.AccountId,
            //            $"Weather risk detected for trip {trip.Title}. Open AI preview to replan.");
            //    }
            //}
        }
    }
}

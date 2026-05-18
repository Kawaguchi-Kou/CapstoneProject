//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Application.DTOs.Requests;
//using Application.Interfaces;
//using Application.Services;
//using Domain.Interfaces;

//namespace Infrastructure.BackgroundJobs
//{
//    public class WeatherMonitorJob : IWeatherMonitorJob
//    {
//        private readonly ITripRepository _tripRepository;
//        private readonly IWeatherRiskScanService _riskScan;
//        private readonly INotificationService _notificationService;

//        public WeatherMonitorJob(
//            ITripRepository tripRepository,
//            IWeatherRiskScanService riskScan,
//            INotificationService noti)
//        {
//            _tripRepository = tripRepository;
//            _riskScan = riskScan;
//            _notificationService = noti;
//        }

//        public async Task ScanUpcomingTripsAsync()
//        {
//            var today = DateTime.Now;

//            var trips = await _tripRepository
//                .GetUpcomingTripsAsync(today);

//            foreach (var trip in trips)
//            {
//                var summary = await _riskScan.ScanAsync(trip.TripId);

//                if (summary.HasHighRisk)
//                {
//                    await _notificationService.CreateNotificationAsync(
//                        new CreateNotificationRequest
//                        {
//                            RecipientId = summary.AccountId,
//                            SenderId = Guid.Empty, // system
//                            Message = "Weather may affect your trip. Open AI preview to replan."
//                        });
//                }
//            }
//        }
//    }
//}

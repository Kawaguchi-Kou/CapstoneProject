using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Interfaces;

namespace Infrastructure.BackgroundJobs
{
    public class WeatherPreloadJob : IWeatherPreloadJob
    {
        private readonly IWeatherService _weatherService;
        private readonly ITripSegmentRepository _segmentRepo; 

        public WeatherPreloadJob(IWeatherService weatherService, ITripSegmentRepository tripSegmentRepository)
        {
            _weatherService = weatherService;
            _segmentRepo = tripSegmentRepository;

        }

        public async Task PreloadTripWeather(Guid tripId)
        {
            // bạn inject thêm repo nếu cần
            var segments = await _segmentRepo.GetByTripIdAsync(tripId);

            foreach (var segment in segments)
            {
                var dates = Enumerable.Range(0,
                        (segment.EndDate.Day - segment.StartDate.Day) + 1)
                    .Select(d => segment.StartDate.AddDays(d))
                    .ToList();

                await _weatherService.PreloadAsync(segment.LocationId, dates);
            }
        }
    }
}

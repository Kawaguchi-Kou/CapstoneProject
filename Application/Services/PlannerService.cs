//using Application.DTOs.Responses;
//using Application.Interfaces;
//using Domain.Entities;
//using Domain.Interfaces;
//using Domain.Weather;

//public class PlannerService : IPlannerService
//{
//    private readonly IPlannerRepository _plannerRepository;
//    private readonly IOpenMeteoService _weatherService;
//    private readonly AdaptiveWeatherRiskEngine _riskEngine;

//    public PlannerService(
//        IPlannerRepository plannerRepository,
//        IOpenMeteoService weatherService,
//        AdaptiveWeatherRiskEngine riskEngine)
//    {
//        _plannerRepository = plannerRepository;
//        _weatherService = weatherService;
//        _riskEngine = riskEngine;
//    }

//    public async Task<PlannerResponse> PlanTripAsync(Guid tripId)
//    {
//        var trip = await _plannerRepository.GetTripWithSegmentsAndItinerary(tripId);
//        if (trip == null)
//            throw new Exception("Trip not found");

//        foreach (var segment in trip.TripSegments.OrderBy(s => s.SequenceNo))
//        {
//            if (!segment.TravelDate.HasValue)
//                continue;

//            // Lấy forecast theo lat/lng của segment
//            var forecast = await _weatherService.GetDailyAsync(
//                segment.FromLatitude,
//                segment.FromLongitude,
//                DateOnly.FromDateTime(segment.TravelDate.Value),
//                DateOnly.FromDateTime(segment.TravelDate.Value)
//            );

//            var weather = forecast.FirstOrDefault();
//            if (weather == null)
//                continue;

//            var risk = _riskEngine.CalculateRisk(
//                weather.PrecipitationProbability,
//                weather.MaxWindSpeed,
//                weather.MaxTemperature
//            );

//            // Update itinerary details thuộc segment đó
//            foreach (var itinerary in trip.Itineraries)
//            {
//                var details = itinerary.ItineraryDetails
//                    .Where(d => d.Date == segment.TravelDate.Value.Date)
//                    .ToList();

//                foreach (var detail in details)
//                {
//                    detail.WeatherRiskScore = risk;
//                }
//            }
//        }

//        await _plannerRepository.SaveChangesAsync();

        
//    }
//}
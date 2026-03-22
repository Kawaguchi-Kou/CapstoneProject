//using Application.DTOs.Responses;
//using Application.Interfaces;
//using Domain.Entities;
//using Domain.Interfaces;
//using Domain.Weather;

//public class PlannerService : IPlannerService
//{
//    private readonly IPOIRepository _poiRepo;
//    private readonly IWeatherForecastRepository _weatherRepo;
//    private readonly IItineraryRepository _itineraryRepo;
//    private readonly IItineraryDetailRepository _detailRepo;
//    private readonly ITripSegmentRepository _segmentRepo;
//    private readonly IAdaptiveWeatherRiskEngine _riskEngine;

//    public PlannerService(
//        IPOIRepository poiRepo,
//        IWeatherForecastRepository weatherRepo,
//        IItineraryRepository itineraryRepo,
//        IItineraryDetailRepository detailRepo,
//        IAdaptiveWeatherRiskEngine riskEngine,
//        ITripSegmentRepository segmentRepo)
//    {
//        _poiRepo = poiRepo;
//        _weatherRepo = weatherRepo;
//        _itineraryRepo = itineraryRepo;
//        _detailRepo = detailRepo;
//        _riskEngine = riskEngine;
//        _segmentRepo = segmentRepo;
//    }

//    public async Task GenerateAsync(Guid tripId)
//    {
//        var segments = await _segmentRepo.GetByTripIdAsync(tripId);

//        foreach (var segment in segments)
//        {
//            var itinerary = new Itinerary
//            {
//                ItineraryId = Guid.NewGuid(),
//                SegmentId = segment.SegmentId,
//                GeneratedByAI = true
//            };

//            await _itineraryRepo.AddAsync(itinerary);

//            var pois = await _poiRepo.GetByLocationAsync(segment.LocationId);

//            var currentDate = segment.StartDate;

//            while (currentDate <= segment.EndDate)
//            {
//                var timeline = GenerateDayTimeline(currentDate);

//                foreach (var slot in timeline)
//                {
//                    var poi = SelectBestPOI(pois, slot);

//                    if (poi == null) continue;

//                    if (!IsValidTimeSlot(poi, slot)) continue;

//                    var forecast = await _weatherRepo
//                        .GetAsync(segment.LocationId, currentDate);

//                    var risk = _riskEngine.CalculateRisk(forecast, poi.IsIndoor);

//                    var detail = new ItineraryDetail
//                    {
//                        DetailId = Guid.NewGuid(),
//                        ItineraryId = itinerary.ItineraryId,
//                        PoiId = poi.Id,
//                        VisitDate = currentDate,
//                        StartTime = slot.Start,
//                        EndTime = slot.End,
//                        WeatherRiskScore = risk
//                    };

//                    await _detailRepo.AddAsync(detail);
//                }

//                currentDate = currentDate.AddDays(1);
//            }
//        }
//    }
//}

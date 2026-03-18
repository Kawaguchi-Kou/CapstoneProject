using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Weather;

public class PlannerService : IPlannerService
{
    private readonly IPlannerRepository _plannerRepository;
    private readonly IOpenMeteoService _weatherService;
    private readonly AdaptiveWeatherRiskEngine _riskEngine;

    public PlannerService(
        IPlannerRepository plannerRepository,
        IOpenMeteoService weatherService,
        AdaptiveWeatherRiskEngine riskEngine)
    {
        _plannerRepository = plannerRepository;
        _weatherService = weatherService;
        _riskEngine = riskEngine;
    }

    //public async Task UpdateItineraryDetail(Guid detailId, UpdateDetailRequest dto)
    //{
    //    var detail = await _repo.GetDetail(detailId);

    //    detail.PoiId = dto.NewPoiId;
    //    detail.VisitDate = dto.NewDate;
    //    detail.IsManualOverride = true;

    //    var weather = await _weatherService.GetForecast(
    //        detail.LocationId,
    //        detail.VisitDate);

    //    detail.WeatherRiskScore =
    //        _riskEngine.CalculateRisk(weather);

    //    await _repo.SaveChanges();
    //}
}

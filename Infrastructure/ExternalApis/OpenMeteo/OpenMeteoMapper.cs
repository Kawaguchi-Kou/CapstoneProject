using Application.DTOs.Weather;

namespace Infrastructure.ExternalApis.OpenMeteo;

public static class OpenMeteoMapper
{
    public static IReadOnlyList<DailyWeatherDto> Map(OpenMeteoDailyResponse response)
    {
        var d = response.Daily;
        var result = new List<DailyWeatherDto>();

        for (int i = 0; i < d.Time.Count; i++)
        {
            result.Add(new DailyWeatherDto
            {
                Date = DateOnly.Parse(d.Time[i]),
                MaxTemperature = d.TemperatureMax[i],
                MaxWindSpeed = d.WindSpeedMax[i],
                PrecipitationProbability = d.PrecipitationProbabilityMax[i]
            });
        }

        return result;
    }
}

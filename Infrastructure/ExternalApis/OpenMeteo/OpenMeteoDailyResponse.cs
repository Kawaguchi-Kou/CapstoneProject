using System.Text.Json.Serialization;

namespace Infrastructure.ExternalApis.OpenMeteo;

/// <summary>
/// Root response từ Open-Meteo daily forecast
/// </summary>
public class OpenMeteoDailyResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("daily")]
    public DailyData Daily { get; set; } = default!;
}

/// <summary>
/// Dữ liệu forecast theo NGÀY
/// </summary>
public class DailyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; set; } = new();

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double> WindSpeedMax { get; set; } = new();

    [JsonPropertyName("precipitation_probability_max")]
    public List<double> PrecipitationProbabilityMax { get; set; } = new();
}

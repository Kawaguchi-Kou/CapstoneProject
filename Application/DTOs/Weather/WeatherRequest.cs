namespace Application.DTOs.Tools;

public class WeatherRequest
{
    /// <summary>
    /// Vĩ độ (ví dụ: 10.8231)
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Kinh độ (ví dụ: 106.6297)
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Ngày bắt đầu (YYYY-MM-DD)
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Ngày kết thúc (YYYY-MM-DD)
    /// </summary>
    public DateOnly EndDate { get; init; }
}

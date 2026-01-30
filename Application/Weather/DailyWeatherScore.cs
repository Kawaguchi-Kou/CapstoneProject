using Application.DTOs.Weather;

namespace Application.Weather;

/// <summary>
/// Scoring chuyên cho PHƯỢT
/// </summary>
public static class DailyWeatherScore
{
    public static double Calculate(DailyWeatherDto d)
    {
        var tempScore = ScoreTemperature(d.MaxTemperature);
        var rainScore = ScoreRain(d.PrecipitationProbability);
        var windScore = ScoreWind(d.MaxWindSpeed);

        // trọng số phượt
        return Math.Round(
            tempScore * 0.4 +
            rainScore * 0.4 +
            windScore * 0.2,
            3);
    }

    // ====== COMPONENT SCORES ======

    // 20–30°C là đẹp nhất
    private static double ScoreTemperature(double t)
    {
        if (t < 15 || t > 38) return 0;

        if (t >= 22 && t <= 30) return 1;

        if (t < 22) return (t - 15) / (22 - 15);
        return (38 - t) / (38 - 30);
    }

    // mưa là sát thương lớn nhất
    private static double ScoreRain(double prob)
    {
        return prob switch
        {
            <= 10 => 1,
            <= 30 => 0.8,
            <= 50 => 0.5,
            <= 70 => 0.2,
            _ => 0
        };
    }

    // phượt chịu gió tốt hơn du lịch thường
    private static double ScoreWind(double wind)
    {
        if (wind <= 10) return 1;
        if (wind <= 20) return 0.8;
        if (wind <= 30) return 0.5;
        return 0.2;
    }
}

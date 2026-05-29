using Application.DTOs.Responses;

public class RouteSuggestionResponse
{
    public string RouteId { get; set; } = "";

    public int RouteIndex { get; set; }

    public List<RouteStopDto> Stops { get; set; } = new();

    public double TotalDistanceKm { get; set; }

    // overall weather description
    public string WeatherSummary { get; set; } = "";

    // AI natural language advice
    public string TravelAdvice { get; set; } = "";

    // suggested activity types
    public List<string> RecommendedActivities { get; set; } = new();

    // warnings
    public List<string> Warnings { get; set; } = new();

    // for Google Maps Polyline
    public List<RoutePolylinePointDto> Polyline { get; set; } = new();
}
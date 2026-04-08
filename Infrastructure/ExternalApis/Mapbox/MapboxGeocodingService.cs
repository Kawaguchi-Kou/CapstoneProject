using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using AutoMapper.Configuration.Conventions;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class MapboxGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MapboxGeocodingService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<(double Latitude, double Longitude)>
            GetCoordinatesAsync(string placeName, string city)
        {
            var token = _configuration["MAPBOX_ACCESS_TOKEN"];

            var searchQuery = $"{placeName}, {city}, Vietnam"; ;

            var url =
                $"https://api.mapbox.com/geocoding/v5/mapbox.places/" +
                $"{Uri.EscapeDataString(searchQuery)}.json" +
                $"?limit=1" +
                $"&country=vn" +
                $"&access_token={token}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Mapbox API error");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            //var features = doc.RootElement.GetProperty("features");
            if (!doc.RootElement.TryGetProperty("features", out var features))
                throw new Exception("Invalid Mapbox response");

            if (features.GetArrayLength() == 0)
                throw new Exception("Location not found");

            var center = features[0].GetProperty("center");

            var longitude = center[0].GetDouble();
            var latitude = center[1].GetDouble();

            return (latitude, longitude);
        }

        public async Task<double> GetDrivingDistance(
        double lat1, double lon1,
        double lat2, double lon2)
        {
            var token = _configuration["MAPBOX_ACCESS_TOKEN"];
            var url = $"https://api.mapbox.com/directions/v5/mapbox/driving/" +
                      $"{lon1},{lat1};{lon2},{lat2}" +
                      $"?access_token={token}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            var distanceMeters = data
                .RootElement
                .GetProperty("routes")[0]
                .GetProperty("distance")
                .GetDouble();

            return distanceMeters / 1000; // convert to km
        }
    }
}

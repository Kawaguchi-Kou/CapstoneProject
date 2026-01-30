using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalApis.OpenMeteo
{
    public class OpenMeteoClient
    {
        private readonly HttpClient _http;

        public OpenMeteoClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<OpenMeteoDailyResponse> GetDailyAsync(
            double lat, double lon)
        {
            var url =
                $"https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}&longitude={lon}" +
                $"&daily=temperature_2m_max,precipitation_probability_mean,windspeed_10m_max" +
                $"&timezone=auto";

            return await _http.GetFromJsonAsync<OpenMeteoDailyResponse>(url)
                   ?? throw new Exception("OpenMeteo failed");
        }
    }
}

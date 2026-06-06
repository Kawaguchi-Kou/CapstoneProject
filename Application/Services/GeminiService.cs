using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;


        public GeminiService(HttpClient http, IConfiguration config, ILogger<GeminiService> logger)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"]!;
            _logger = logger;
        }

        public async Task<string> GenerateAsync(string prompt)
        {
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var response = await _http.PostAsJsonAsync(url, request);

            var errorBody = await response.Content.ReadAsStringAsync();

            _logger.LogError(
                "Gemini returned {StatusCode}. Response: {Body}",
                response.StatusCode,
                errorBody);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return ExtractText(json);
        }

        private string ExtractText(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);

                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}

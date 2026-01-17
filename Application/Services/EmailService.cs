using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;


namespace Application.Services
{
    public class EmailService: IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subjects, string body)
        {
            using var client = new HttpClient();
            var apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var payload = new
            {
                sender = new { name = "TravelPlanner", email = "haonc.t2.1922@gmail.com" },
                to = new[] { new { email = toEmail } },
                subject = subjects,
                htmlContent = body
            };

            var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send email: {response.StatusCode} - {errorContent}");
            }
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class ShareTripRequest
    {
        [Required]
        public string FrontendBaseUrl { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class PreferenceRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}

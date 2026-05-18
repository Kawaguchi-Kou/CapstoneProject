using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class UpdatePartnerProfileDto
    {
        [MaxLength(255)]
        public string? BusinessName { get; set; }

        [MaxLength(500)]
        public string? BusinessAddress { get; set; }

        [MaxLength(20)]
        public string? BusinessPhone { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        public string? BusinessEmail { get; set; }
    }
}

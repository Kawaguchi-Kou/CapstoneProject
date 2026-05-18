using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class CreatePartnerRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BusinessAddress { get; set; } = string.Empty;

        [MaxLength(20)]
        public string BusinessPhone { get; set; } = string.Empty;

        [MaxLength(255)]
        [EmailAddress]
        public string BusinessEmail { get; set; } = string.Empty;

        /// <summary>
        /// File giấy phép kinh doanh (upload trực tiếp qua form)
        /// </summary>
        public IFormFile? BusinessLicenseFile { get; set; }
    }
}

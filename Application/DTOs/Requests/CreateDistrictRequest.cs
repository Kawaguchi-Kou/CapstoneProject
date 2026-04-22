using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class CreateDistrictRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid LocationId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Requests
{
    public class CreateProfileRequest
    {
        public Guid Id { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public IFormFile? AvatarUrl { get; set; }
        public string Gender { get; set; } = string.Empty;
    }
}

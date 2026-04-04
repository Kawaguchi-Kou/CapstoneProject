using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class UpdateAccountRequest
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}

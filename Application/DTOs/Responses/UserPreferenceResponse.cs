using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Responses
{
    public class UserPreferenceResponse
    {
        public Guid Id { get; set; }
        public Guid PreferenceId { get; set; }
        public string PreferenceName { get; set; } = string.Empty;
    }
}

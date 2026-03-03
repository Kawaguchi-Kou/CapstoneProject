using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class UserPreferencesRequest
    {
        public List<Guid> PreferenceIds { get; set; }
    }
}

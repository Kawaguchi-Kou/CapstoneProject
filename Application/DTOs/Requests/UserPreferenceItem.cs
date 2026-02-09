using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Requests
{
    public class UserPreferenceItem
    {
        public string PreferenceCode { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}

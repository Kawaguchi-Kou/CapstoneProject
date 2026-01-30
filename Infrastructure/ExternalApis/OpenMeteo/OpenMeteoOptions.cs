using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalApis.OpenMeteo
{
    public class OpenMeteoOptions
    {
        public const string SectionName = "OpenMeteo";

        public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";
    }
}

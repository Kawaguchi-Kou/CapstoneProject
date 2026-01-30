using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Tools
{
    public static class McpToolSchemas
    {
        public static object GetWeather => new
        {
            name = "get_weather",
            description = "Get daily weather forecast",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    req = new
                    {
                        type = "object",
                        properties = new
                        {
                            latitude = new { type = "number" },
                            longitude = new { type = "number" },
                            startDate = new { type = "string", format = "date" },
                            endDate = new { type = "string", format = "date" }
                        },
                        required = new[] { "latitude", "longitude", "startDate", "endDate" }
                    }
                },
                required = new[] { "req" }
            }
        };

        public static object GetDistance => new
        {
            name = "get_distance",
            description = "Get travel distance and duration",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    origin = new { type = "string" },
                    destination = new { type = "string" }
                },
                required = new[] { "origin", "destination" }
            }
        };
    }
}

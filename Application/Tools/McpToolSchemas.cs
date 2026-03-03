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

        public static object GetCoordinates => new
        {
            name = "get_coordinates",
            description = "Get latitude and longitude from a place name in Vietnam",
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
                            placeName = new
                            {
                                type = "string",
                                description = "Name of the place to geocode"
                            },
                            city = new
                            {
                                type = "string",
                                description = "City where the place is located (optional)"
                            }
                        },
                        required = new[] { "placeName" } // city NOT required
                    }
                },
                required = new[] { "req" }
            }
        };
    }
}

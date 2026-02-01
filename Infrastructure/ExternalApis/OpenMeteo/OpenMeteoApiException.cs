using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalApis.OpenMeteo
{
    public class OpenMeteoApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public OpenMeteoApiException(HttpStatusCode code, string body) : base(body)
        {
            StatusCode = code;
        }
    }
}

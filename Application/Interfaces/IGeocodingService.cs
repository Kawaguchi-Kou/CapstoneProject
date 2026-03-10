using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IGeocodingService
    {
        Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string address, string city);
    }
}

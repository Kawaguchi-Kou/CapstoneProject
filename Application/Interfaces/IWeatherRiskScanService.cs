using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;

namespace Application.Interfaces
{
    public interface IWeatherRiskScanService
    {
        Task<TripRiskScanResponse> ScanAsync(Guid tripId);
    }
}

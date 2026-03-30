using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBackgroundJobService
    {
        void EnqueueWeatherPreload(Guid tripId);
    }
}

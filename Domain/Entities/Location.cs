using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Location
    {
        [Key]
        public Guid LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        //Navigation
        public ICollection<WeatherForecast> WeatherForecast { get; set; }
        public ICollection<TripSegment> Segments { get; set; }
    }
}

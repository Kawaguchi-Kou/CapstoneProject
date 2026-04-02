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
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        //Navigation
        public ICollection<POI> POIs { get; set; }
        public ICollection<WeatherForecast> WeatherForecast { get; set; }
        public ICollection<TripSegment> Segments { get; set; }
        public ICollection<District> Districts { get; set; }
    }
}

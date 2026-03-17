using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class POI
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ApproxCost { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = string.Empty;
        public string GoogleMapLink { get; set; } = string.Empty;
        public bool IsIndoor { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Required]
        public Guid LocationId { get; set; }

        public string? POIImgUrl { get; set; }

        //Navigation
        [ForeignKey(nameof(LocationId))]
        public Location Location { get; set; }
        public ICollection<POIPreference> PoiPreferences { get; set; }
        public ICollection<ItineraryDetail> ItineraryDetails { get; set; }
    }
}

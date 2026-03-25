using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

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
        public TimeOnly? OpenHour { get; set; }
        public TimeOnly? CloseHour { get; set; }
        public bool Is24Hours { get; set; }
        public string? VisitRecommendation { get; set; }
        public string GoogleMapLink { get; set; } = string.Empty;
        public string? POIImgUrl { get; set; } 
        public bool IsIndoor { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public POIStatus Status { get; set; }
        public Guid? PartnerId { get; set; }
        [Required]
        public Guid LocationId { get; set; }

        //Navigation
        [ForeignKey(nameof(LocationId))]
        public Location Location { get; set; }
        [ForeignKey(nameof(PartnerId))]
        public Account Partner { get; set; }
        public ICollection<POIPreference> PoiPreferences { get; set; }
        public ICollection<ItineraryDetail> ItineraryDetails { get; set; }
        public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
    }
}

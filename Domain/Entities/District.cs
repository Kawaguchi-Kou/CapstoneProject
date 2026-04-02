using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class District
    {
        public Guid Id { get; set; } = new Guid();
        public string Name { get; set; } = string.Empty;
        [Required]
        public Guid LocationId { get; set; }
        // Navigation
        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; } 
        public ICollection<POI> POIs { get; set; } = new List<POI>();
    }
}

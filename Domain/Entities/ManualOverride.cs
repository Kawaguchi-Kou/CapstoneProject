using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ManualOverride
    {
        [Key]
        public Guid Id { get; set; }
        public bool WarningShown { get; set; }
        public bool UserConfirmed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public Guid DetailId { get; set; }

        [ForeignKey("DetailId")]
        public ItineraryDetail? Detail { get; set; }
    }
}

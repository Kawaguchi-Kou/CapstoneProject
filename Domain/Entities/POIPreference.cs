using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class POIPreference
    {
        public Guid PoiId { get; set; }
        public POI POI { get; set; }

        public Guid PreferenceId { get; set; }
        public Preference Preference { get; set; }
    }
}

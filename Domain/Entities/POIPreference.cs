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

        //public double Weight { get; set; } // mức độ phù hợp (0–1 hoặc 0–100)
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Constants
{
    public static class PreferenceWeights
    {
        private static readonly Dictionary<string, double> _weights = new()
        {
            { "Food", 1.0 },
            { "Relax", 0.9 },
            { "Culture", 1.1 },
            { "Adventure", 1.2 },
            { "Nature", 1.0 },
            { "Shopping", 0.8 },
            { "Nightlife", 0.9 },
            { "Luxury", 0.3 },
            { "Budget", 1.1 }
        };

        public static double Get(string preferenceCode)
            => _weights.TryGetValue(preferenceCode, out var w) ? w : 1.0;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Weather
{
    public class WeatherScorer
    {
        public double Score(double temp, double wind, int rain)
        {
            // HARD SAFETY — không thương lượng
            if (wind >= 30 || rain >= 85)
                return 0;

            return
                0.35 * TemperatureScore(temp) +
                0.40 * WindScore(wind) +
                0.25 * RainScore(rain);
        }

        private double TemperatureScore(double T)
        {
            const double T_MIN = 15;
            const double T_OPT_LOW = 22;
            const double T_OPT_HIGH = 34;
            const double T_MAX = 42;

            if (T <= T_MIN || T >= T_MAX) return 0;
            if (T < T_OPT_LOW) return (T - T_MIN) / (T_OPT_LOW - T_MIN);
            if (T <= T_OPT_HIGH) return 1;
            return (T_MAX - T) / (T_MAX - T_OPT_HIGH);
        }

        private double WindScore(double W)
        {
            const double W_OPT = 8;
            const double W_DANGER = 25;

            if (W <= W_OPT) return 1;
            if (W >= W_DANGER) return 0;
            return 1 - (W - W_OPT) / (W_DANGER - W_OPT);
        }

        private double RainScore(int R)
        {
            if (R <= 30) return 1;
            if (R >= 80) return 0;
            return 1 - (R - 30) / 50.0;
        }
    }
}

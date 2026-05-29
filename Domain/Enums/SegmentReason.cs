using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum SegmentReason
    {
        GoodWeather,          // thời tiết đẹp
        AvoidRain,            // tránh mưa
        BadWeather,           // thời tiết xấu
        ShortDistance,        // quãng đường ngắn
        LongDistance,         // quá xa
        ScenicRoute,          // cung đường đẹp
        DangerousWeather,     // mưa lớn / sương mù
        TimeNotEnough,        // không đủ thời gian
        FitsUserPreference,   // hợp sở thích
        PartnerBoost          // quảng cáo
    }
}

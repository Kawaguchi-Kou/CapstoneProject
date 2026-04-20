using Application.DTOs.Responses;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/poi-types")]
    public class POITypeController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            var options = Enum.GetValues<POIType>()
                .Select(type => new PoiTypeOptionResponse
                {
                    Value = type.ToString(),
                    Label = GetLabel(type)
                })
                .ToList();

            return Ok(options);
        }

        private static string GetLabel(POIType type)
        {
            return type switch
            {
                POIType.Restaurant => "Nhà hàng",
                POIType.Attraction => "Điểm tham quan",
                POIType.Cafe => "Quán cà phê",
                POIType.Hotel => "Khách sạn",
                POIType.Museum => "Bảo tàng",
                POIType.Park => "Công viên",
                POIType.Shopping => "Mua sắm",
                POIType.StreetFood => "Ẩm thực đường phố",
                POIType.Landmark => "Biểu tượng nổi bật",
                POIType.Viewpoint => "Điểm ngắm cảnh",
                POIType.Beach => "Bãi biển",
                POIType.CulturalSite => "Di tích văn hóa",
                POIType.HistoricalSite => "Di tích lịch sử",
                POIType.Temple => "Chùa/Đền",
                POIType.Church => "Nhà thờ",
                POIType.Nature => "Thiên nhiên",
                POIType.Waterfall => "Thác nước",
                POIType.Market => "Chợ",
                POIType.NightMarket => "Chợ đêm",
                POIType.Bar => "Quán bar",
                POIType.Nightlife => "Giải trí về đêm",
                POIType.Resort => "Khu nghỉ dưỡng",
                _ => type.ToString()
            };
        }
    }
}

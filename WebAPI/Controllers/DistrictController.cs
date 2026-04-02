using System.ComponentModel.Design;
using System.Security.Claims;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/districts")]
    public class DistrictController : Controller
    {
        private readonly IDistrictService _districtSerivce;
        private readonly IMapper _mapper;

        public DistrictController(IDistrictService districtSerivce, IMapper mapper)
        {
            _districtSerivce = districtSerivce;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var district = await _districtSerivce.GetAllAsync();
                var response = _mapper.Map<List<DistrictResponse>>(district);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

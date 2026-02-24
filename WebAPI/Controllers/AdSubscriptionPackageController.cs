using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/ad-subscription-packages")]
    public class AdSubscriptionPackageController : ControllerBase
    {
        private readonly IAdSubscriptionPackageService _packageService;
        private readonly IMapper _mapper;

        public AdSubscriptionPackageController(IAdSubscriptionPackageService packageService, IMapper mapper)
        {
            _packageService = packageService;
            _mapper = mapper;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePackage([FromBody] CreateAdSubscriptionPackageRequest request)
        {
            try
            {
                // Kiểm tra role Admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Admin.", role = userRole });
                }

                var package = await _packageService.CreatePackageAsync(request);
                var response = _mapper.Map<AdSubscriptionPackageResponse>(package);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPackageById(Guid id)
        {
            try
            {
                var package = await _packageService.GetPackageByIdAsync(id);
                if (package == null)
                    return NotFound(new { message = "Package not found" });

                // Partner chỉ xem được gói active, Admin xem được tất cả
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin" && package.Status.ToLower() != "active")
                {
                    return NotFound(new { message = "Package not found" });
                }

                var response = _mapper.Map<AdSubscriptionPackageResponse>(package);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllPackages()
        {
            try
            {
                var packages = await _packageService.GetAllPackagesAsync();
                
                // Partner chỉ xem được gói active, Admin xem được tất cả
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    packages = packages.Where(p => p.Status.ToLower() == "active").ToList();
                }

                var responses = _mapper.Map<List<AdSubscriptionPackageResponse>>(packages);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] CreateAdSubscriptionPackageRequest request)
        {
            try
            {
                // Kiểm tra role Admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Admin.", role = userRole });
                }

                var package = await _packageService.UpdatePackageAsync(id, request);
                var response = _mapper.Map<AdSubscriptionPackageResponse>(package);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            try
            {
                // Kiểm tra role Admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Admin.", role = userRole });
                }

                var result = await _packageService.DeletePackageAsync(id);
                if (!result)
                    return NotFound(new { message = "Package not found" });

                return Ok(new { message = "Package deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

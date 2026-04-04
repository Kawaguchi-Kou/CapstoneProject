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
        public async Task<IActionResult> GetAllPackages([FromQuery] int page = 1)
        {
            try
            {
                const int pageSize = 15;
                if (page < 1) page = 1;

                var packages = await _packageService.GetAllPackagesAsync();
                
                // Partner chỉ xem được gói active, Admin xem được tất cả
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    packages = packages.Where(p => p.Status.ToLower() == "active").ToList();
                }

                var pagedPackages = packages
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var responses = _mapper.Map<List<AdSubscriptionPackageResponse>>(pagedPackages);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("filter")]
        [Authorize]
        public async Task<IActionResult> FilterPackages(
            [FromQuery] string? title,
            [FromQuery] string? status,
            [FromQuery] string? sortPrice)
        {
            try
            {
                var packages = await _packageService.GetFilteredPackagesAsync(title, status, sortPrice);

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

        [HttpPut("{id}/activate")]
        [Authorize]
        public async Task<IActionResult> ActivatePackage(Guid id)
        {
            try
            {
                // Kiểm tra role Admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Admin.", role = userRole });
                }

                await _packageService.ActivatePackageAsync(id);
                return Ok(new { message = "Package activated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/deactivate")]
        [Authorize]
        public async Task<IActionResult> DeactivatePackage(Guid id)
        {
            try
            {
                // Kiểm tra role Admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim();
                if (userRole != "Admin")
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện thao tác này. Yêu cầu role Admin.", role = userRole });
                }

                await _packageService.DeactivatePackageAsync(id);
                return Ok(new { message = "Package deactivated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            try
            {
                await _packageService.ImportPackagesExcelAsync(file);
                return Ok("Import packages success");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var fileContent = await _packageService.ExportPackagesExcelAsync();
                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "AdSubscriptionPackages.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

}


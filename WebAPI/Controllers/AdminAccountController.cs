using Application.Interfaces;
using Application.Services;
using Application.DTOs.Responses;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;

        public AdminAccountsController(IAdminService adminService, IMapper mapper)
        {
            _adminService = adminService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            try
            {
                var accounts = await _adminService.GetAll();
                return Ok(accounts);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterAccounts(
            [FromQuery] string? roleName,
            [FromQuery] string? status,
            [FromQuery] string? name)
        {
            try
            {
                bool? isActive = null;
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Status", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true;
                    }
                    else if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = false;
                    }
                }

                var accounts = await _adminService.GetFilteredAccountsAsync(roleName, isActive, name);
                var responses = _mapper.Map<List<AccountResponse>>(accounts);
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateAccount(Guid id) {

            try
            {
                await _adminService.ActivateAccount(id);
                return Ok("Account activated");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            
        }
        
            
        

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(Guid id)
        {
            try
            {
                await _adminService.DeactivateAccount(id);
                return Ok("Account deactivated");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}
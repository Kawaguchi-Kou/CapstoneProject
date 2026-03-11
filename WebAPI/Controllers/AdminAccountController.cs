using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminAccountsController(IAdminService adminService)
        {
            _adminService = adminService;
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
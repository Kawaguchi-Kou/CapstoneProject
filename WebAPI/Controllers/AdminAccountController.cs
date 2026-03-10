using Application.Interfaces;
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
            var accounts = await _adminService.GetAll();
            return Ok(accounts);
        }
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateAccount(Guid id)
        {
            await _adminService.ActivateAccount(id);
            return Ok("Account activated");
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(Guid id)
        {
            await _adminService.DeactivateAccount(id);
            return Ok("Account deactivated");
        }
    }
}
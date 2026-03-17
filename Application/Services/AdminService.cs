using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAuthRepository _authRepository;

        public AdminService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<List<Account>> GetAll()
        {
            var accounts = await _authRepository.GetAllAccountsAsync();

            return accounts;
        }

        public async Task<List<Account>> GetFilteredAccountsAsync(string? roleName, bool? isActive, string? name)
        {
            var accounts = await _authRepository.GetFilteredAccountsAsync(roleName, isActive, name);

            return accounts;
        }

        public async Task ActivateAccount(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);

            if (account == null)
                throw new Exception("Account not found");

            account.IsActive = true;

            await _authRepository.SaveChangesAsync();
        }

        public async Task DeactivateAccount(Guid id)
        {
            var account = await _authRepository.GetByIdAsync(id);

            if (account == null)
                throw new Exception("Account not found");

            account.IsActive = false;

            await _authRepository.SaveChangesAsync();
        }
    }
}
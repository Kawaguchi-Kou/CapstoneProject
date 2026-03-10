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
    }
}
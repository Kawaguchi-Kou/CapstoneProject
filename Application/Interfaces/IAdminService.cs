using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task<List<AccountResponse>> GetAll();
        Task<List<AccountResponse>> GetFilteredAccountsAsync(string? roleName, bool? isActive, string? name);

        Task<AccountResponse> GetById(Guid id);

        Task<AccountResponse> CreateAccount(CreateAccountRequest request);
        Task<AccountResponse> UpdateAccount(UpdateAccountRequest request);
        Task DeleteAccount(Guid id);

        Task ActivateAccount(Guid id);
        Task DeactivateAccount(Guid id);

        Task<byte[]> ExportAccountsExcelAsync();
        Task ImportAccountsExcelAsync(IFormFile file);
    }
}
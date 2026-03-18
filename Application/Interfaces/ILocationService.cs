using Application.DTOs.Requests;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILocationService
    {
        Task<List<LocationResponse>> GetAllAsync();
        Task<LocationResponse?> GetByIdAsync(Guid id);
        Task<Location> CreateAsync(CreateLocationRequest request);
        Task<Location> UpdateAsync(Guid id, UpdateLocationRequest request);
        Task DeleteAsync(Guid id);

        Task ImportExcelAsync(IFormFile file);
    }
}

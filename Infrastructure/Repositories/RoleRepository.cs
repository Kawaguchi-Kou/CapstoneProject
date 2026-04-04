using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Role?> GetByNameIgnoreCaseAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            var normalized = roleName.Trim().ToLower();
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == normalized);
        }
    }
}

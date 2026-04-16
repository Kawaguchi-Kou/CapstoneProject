using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Configurations;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts.Include(a => a.Role).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Account?> GetByNameAsync(string name)
        {
            return await _context.Accounts.Include(a => a.Role).FirstOrDefaultAsync(u => u.Name == name);
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts
                            .Include(a => a.Role)
                            .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Account>> GetAllAccountsAsync()
        {
            return await _context.Accounts.Include(a => a.Role).ToListAsync();
        }

        public async Task<List<Account>> GetFilteredAccountsAsync(string? roleName, bool? isActive, string? name)
        {
            var query = _context.Accounts
                .Include(a => a.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var normalizedRole = roleName.Trim().ToLower();
                query = query.Where(a => a.Role != null && a.Role.Name != null && a.Role.Name.ToLower().Contains(normalizedRole));
            }

            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = name.Trim().ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(normalizedName));
            }

            return await query.ToListAsync();
        }

    }
}

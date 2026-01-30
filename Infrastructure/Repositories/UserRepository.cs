using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;

namespace Infrastructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository (AppDbContext context)
        {
            _context = context;
        }
        
        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        public async Task<List<Account>> GetAllAsync()
        {
            return await _context.Accounts.ToListAsync();
        }

        public async Task<Account> UpdateProfileAsync(Account user)
        {
            _context.Accounts.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<Account> CreateProfileAsync(Account user)
        {
            await _context.Accounts.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<List<Account>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.Accounts
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
        }
    }
}

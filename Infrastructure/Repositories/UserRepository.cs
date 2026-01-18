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
        
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> UpdateProfileAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> CreateProfileAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<List<User>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
        }
    }
}

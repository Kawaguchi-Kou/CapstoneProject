using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;

namespace Infrastructure.Repositories
{
    public class PreferenceRepository : IPreferenceRepository
    {
        private readonly AppDbContext _context;

        public PreferenceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Preference>> GetAllAsync()
        {
            return await _context.Preferences
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetUserPreferenceIdsAsync(Guid accountId)
        {
            return await _context.UserPreferences
                .Where(up => up.AccountId == accountId)
                .Select(up => up.PreferenceId)
                .ToListAsync();
        }

        public async Task<Preference?> GetByIdAsync(Guid id)
        {
            return await _context.Preferences.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Preference?> GetByNameAsync(string name)
        {
            return await _context.Preferences.FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task AddAsync(Preference preference)
        {
            await _context.Preferences.AddAsync(preference);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Preference preference)
        {
            _context.Preferences.Update(preference);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Preference preference)
        {
            _context.Preferences.Remove(preference);
            await _context.SaveChangesAsync();
        }
    }
}

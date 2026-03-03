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

        public async Task<List<UserPreference>> GetPreferenceByAccountIdAsync(Guid accountId)
        {
            return await _context.UserPreferences
                .Where(x => x.AccountId == accountId)
                .ToListAsync();
        }

        //public async Task UpsertAsync(Guid accountId, List<UserPreference> preferences)
        //{
        //    var existing = await _context.UserPreferences
        //        .Where(x => x.AccountId == accountId)
        //        .ToListAsync();

        //    foreach (var pref in preferences)
        //    {
        //        //var current = existing.FirstOrDefault(x =>
        //        //    x.PreferenceCode == pref.PreferenceCode);

        //        //if (current != null)
        //        //{
        //        //    // update
        //        //    current.Score = pref.Score;
        //        //}
        //        //else
        //        //{
        //        //    // insert
        //        //    _context.UserPreferenceVectors.Add(pref);
        //        //}
        //    }

        //    await _context.SaveChangesAsync();
        //}

        //public async Task UpdateUserPreferences(Guid accountId, List<Guid> preferenceIds)
        //{
        //    var existing = await _context.UserPreferences
        //        .Where(x => x.AccountId == accountId)
        //        .ToListAsync();

        //    _context.UserPreferences.RemoveRange(existing);

        //    var newPreferences = preferenceIds.Select(id => new UserPreference
        //    {
        //        Id = Guid.NewGuid(),
        //        AccountId = accountId,
        //        PreferenceId = id
        //    });

        //    await _context.UserPreferences.AddRangeAsync(newPreferences);
        //    await _context.SaveChangesAsync();
        //}

        public async Task ReplaceUserPreferences(
            Guid accountId,
            List<Guid> preferenceIds)
        {
            var existing = await _context.UserPreferences
                .Where(x => x.AccountId == accountId)
                .ToListAsync();

            _context.UserPreferences.RemoveRange(existing);

            var newPreferences = preferenceIds.Select(id => new UserPreference
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PreferenceId = id
            });

            await _context.UserPreferences.AddRangeAsync(newPreferences);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

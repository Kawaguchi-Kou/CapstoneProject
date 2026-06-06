using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPreferenceRepository
    {
        Task<List<Preference>> GetAllAsync();
        Task<List<Guid>> GetUserPreferenceIdsAsync(Guid accountId);
        Task<Preference?> GetByIdAsync(Guid id);
        Task<Preference?> GetByNameAsync(string name);
        Task AddAsync(Preference preference);
        Task UpdateAsync(Preference preference);
        Task DeleteAsync(Preference preference);
    }
}

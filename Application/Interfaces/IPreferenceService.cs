using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPreferenceService
    {
        Task<List<Preference>> GetAllPreferencesAsync();
        Task<Preference?> GetPreferenceByIdAsync(Guid id);
        Task<Preference> CreatePreferenceAsync(string name);
        Task<Preference> UpdatePreferenceAsync(Guid id, string name);
        Task DeletePreferenceAsync(Guid id);
    }
}

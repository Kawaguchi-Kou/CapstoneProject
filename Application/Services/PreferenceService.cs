using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class PreferenceService : IPreferenceService
    {
        private readonly IPreferenceRepository _preferenceRepository;

        public PreferenceService(IPreferenceRepository preferenceRepository)
        {
            _preferenceRepository = preferenceRepository;
        }

        public async Task<List<Preference>> GetAllPreferencesAsync()
        {
            return await _preferenceRepository.GetAllAsync();
        }

        public async Task<Preference?> GetPreferenceByIdAsync(Guid id)
        {
            return await _preferenceRepository.GetByIdAsync(id);
        }

        public async Task<Preference> CreatePreferenceAsync(string name)
        {
            var normalizedName = name.Trim();
            var existing = await _preferenceRepository.GetByNameAsync(normalizedName);
            if (existing != null)
                throw new Exception("Preference with this name already exists");

            var preference = new Preference
            {
                Id = Guid.NewGuid(),
                Name = normalizedName
            };

            await _preferenceRepository.AddAsync(preference);
            return preference;
        }

        public async Task<Preference> UpdatePreferenceAsync(Guid id, string name)
        {
            var preference = await _preferenceRepository.GetByIdAsync(id);
            if (preference == null)
                throw new KeyNotFoundException("Preference not found");

            var normalizedName = name.Trim();
            var existing = await _preferenceRepository.GetByNameAsync(normalizedName);
            if (existing != null && existing.Id != id)
                throw new Exception("Preference with this name already exists");

            preference.Name = normalizedName;
            await _preferenceRepository.UpdateAsync(preference);
            return preference;
        }

        public async Task DeletePreferenceAsync(Guid id)
        {
            var preference = await _preferenceRepository.GetByIdAsync(id);
            if (preference == null)
                throw new KeyNotFoundException("Preference not found");

            await _preferenceRepository.DeleteAsync(preference);
        }
    }
}

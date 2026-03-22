using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ItineraryDetailRepository : IItineraryDetailRepository
    {
        private readonly AppDbContext _context;

        public ItineraryDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ItineraryDetail>> GetByItineraryIdAsync(Guid itineraryId)
        {
            return await _context.ItineraryDetails
                .Where(x => x.ItineraryId == itineraryId)
                .OrderBy(x => x.VisitDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync();
        }

        public async Task<ItineraryDetail?> GetByIdAsync(Guid id)
        {
            return await _context.ItineraryDetails
                .FirstOrDefaultAsync(x => x.DetailId == id);
        }

        public async Task AddAsync(ItineraryDetail detail)
        {
            await _context.ItineraryDetails.AddAsync(detail);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<ItineraryDetail> details)
        {
            await _context.ItineraryDetails.AddRangeAsync(details);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ItineraryDetail detail)
        {
            _context.ItineraryDetails.Update(detail);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(List<ItineraryDetail> details)
        {
            _context.ItineraryDetails.UpdateRange(details);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByItineraryIdAsync(Guid itineraryId)
        {
            var details = await _context.ItineraryDetails
                .Where(x => x.ItineraryId == itineraryId)
                .ToListAsync();

            _context.ItineraryDetails.RemoveRange(details);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ItineraryDetail>> GetHighRiskDetailsAsync(
            Guid itineraryId,
            double threshold)
        {
            return await _context.ItineraryDetails
                .Where(x =>
                    x.ItineraryId == itineraryId &&
                    !x.IsManualOverride && // ⭐ respect user override
                    x.WeatherRiskScore >= threshold)
                .OrderByDescending(x => x.WeatherRiskScore)
                .ToListAsync();
        }
    }
}

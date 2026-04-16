using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.EntitiesConfigurations;

namespace Infrastructure.Repositories
{
    public class ParticipantRepository : IParticipantRepository
    {
        private readonly AppDbContext _context;

        public ParticipantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Participant?> GetByUserIdAndTripIdAsync(Guid userId, Guid tripId)
        {
            return await _context.Participants
                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.TripId == tripId);
        }

        public async Task<Participant> AddTripParticipantAsync(Participant participant)
        {
            var entity = await _context.Participants.AddAsync(participant);
            await _context.SaveChangesAsync();
            return entity.Entity;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> AmountParticipantsInTripAsync(Guid tripId)
        {
            return await _context.Participants
                .CountAsync(sp => sp.TripId == tripId && sp.Status == ParticipantStatus.Active);
        }

        public async Task<List<Participant>> GetAllParticipantByTripIdAsync(Guid tripId)
        {
            return await _context.Participants
                .Where(sp => sp.TripId == tripId)
                .ToListAsync();
        }

        public async Task<Participant?> GetParticipantByUserIdAsync(Guid userId)
        {
            return await _context.Participants
                .Where(sp => sp.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<Participant?> GetParticipantByIdAsync(Guid participantId)
        {
            return await _context.Participants
                .Where(sp => sp.Id == participantId).FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(Guid tripId, Guid userId)
        {
            return await _context.Participants
                .AnyAsync(x => x.TripId == tripId && x.UserId == userId);
        }
    }
}

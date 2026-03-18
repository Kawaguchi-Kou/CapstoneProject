using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IParticipantRepository
    {
        Task<Participant?> GetByUserIdAndTripIdAsync(Guid userId, Guid tripId);
        Task<Participant> AddTripParticipantAsync(Participant participant);
        Task<List<Participant>> GetAllTripByParticipantIdAsync(Guid participantId);
        Task SaveChangesAsync();
    }
}

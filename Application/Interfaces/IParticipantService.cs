using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IParticipantService
    {
        Task<Participant?> GetByUserIdAndTripIdAsync(Guid userId, Guid tripId);
        Task<Participant> AddTripParticipantAsync(Guid tripId, AddParticipantRequest request, Guid requesterId);
        Task<List<Participant>> GetAllParticipantByTripIdAsync(Guid tripId);
        Task<string> GenerateInviteLinkAsync(Guid tripId, Guid requesterId);
        Task<Participant> JoinByTripIdAsync(Guid tripId, Guid userId);
        Task<(string link, byte[] qrImage)> GenerateInviteQrAsync(Guid tripId, Guid requesterId);
    }
}

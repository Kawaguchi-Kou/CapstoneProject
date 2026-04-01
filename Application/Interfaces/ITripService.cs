using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface ITripService
    {
        Task<Trip> CreateTripAsync(Trip newTrip);
        Task<(string InviteUrl, string QrCodeBase64)> GenerateShareLinkAsync(string frontendBaseUrl, Guid tripId, Guid userId);
        Task JoinTripAsync(string token, Guid userId);
        Task<ParticipantRole?> GetUserRoleInTripAsync(Guid tripId, Guid userId);
    }
}

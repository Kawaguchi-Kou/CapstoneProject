using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly IParticipantRepository _participantRepo;
        private readonly IAuthRepository _accountRepo;
        private readonly ITripRepository _tripRepo;

        public ParticipantService(
            IParticipantRepository participantRepo,
            IAuthRepository accountRepo,
            ITripRepository tripRepo)
        {
            _participantRepo = participantRepo;
            _accountRepo = accountRepo;
            _tripRepo = tripRepo;
        }

        // 🔷 Owner adds user
        public async Task<Participant> AddTripParticipantAsync(Guid tripId, AddParticipantRequest request, Guid requesterId)
        {
            var trip = await _tripRepo.GetByIdAsync(tripId);

            if (trip == null || trip.OwnerId != requesterId)
                throw new UnauthorizedAccessException("Not owner");

            Account? user = null;

            if (request.UserId.HasValue)
                user = await _accountRepo.GetByIdAsync(request.UserId.Value);
            else if (!string.IsNullOrEmpty(request.Email))
                user = await _accountRepo.GetByEmailAsync(request.Email);
            else if (!string.IsNullOrEmpty(request.Username))
                user = await _accountRepo.GetByNameAsync(request.Username);

            if (user == null)
                throw new Exception("User not found");

            var exists = await _participantRepo.ExistsAsync(tripId, user.Id);
            if (exists)
                throw new Exception("Already in trip");

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                TripId = tripId,
                UserId = user.Id,
                Status = request.AutoAccept
                    ? ParticipantStatus.Active
                    : ParticipantStatus.Inactive,
                Role = ParticipantRole.Viewer
            };

            await _participantRepo.AddTripParticipantAsync(participant);
            return participant;
        }

        // 🔷 Generate QR link (simple)
        public async Task<string> GenerateInviteLinkAsync(Guid tripId, Guid requesterId)
        {
            var trip = await _tripRepo.GetByIdAsync(tripId);

            if (trip == null || trip.OwnerId != requesterId)
                throw new UnauthorizedAccessException();

            return $"https://yourapp.com/join?tripId={tripId}";
        }

        // 🔷 Join via QR (tripId)
        public async Task<Participant> JoinByTripIdAsync(Guid tripId, Guid userId)
        {
            var exists = await _participantRepo.ExistsAsync(tripId, userId);

            if (exists)
                throw new Exception("Already joined");

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                TripId = tripId,
                UserId = userId,
                Status = ParticipantStatus.Active,
                Role = ParticipantRole.Viewer
            };

            await _participantRepo.AddTripParticipantAsync(participant);
            return participant;
        }

        public async Task<Participant?> GetByUserIdAndTripIdAsync(Guid userId, Guid tripId)
        {
            var participant = await _participantRepo.GetByUserIdAndTripIdAsync(userId, tripId);
            return participant;
        }

        public async Task<List<Participant>> GetAllParticipantByTripIdAsync(Guid tripId)
        {
            var participants = await _participantRepo.GetAllParticipantByTripIdAsync(tripId);
            return participants;
        }

    }
}

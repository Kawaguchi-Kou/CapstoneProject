using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services
{
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IBackgroundJobService _jobService;
        private readonly IQRCodeGenerator _qrCodeGenerator;
        private readonly IParticipantRepository _participantRepo;

        public TripService(ITripRepository tripRepo, IBackgroundJobService jobService, IQRCodeGenerator qrCodeGenerator, IParticipantRepository participantRepo)
        {
            _tripRepo = tripRepo;
            _jobService = jobService;
            _qrCodeGenerator = qrCodeGenerator;
            _participantRepo = participantRepo;
        }


        public async Task<Trip> CreateTripAsync(Trip newTrip)
        {
            newTrip.Status = TripStatus.InProgress;
            newTrip.CreatedAt = DateTime.UtcNow;
            await _tripRepo.AddAsync(newTrip);
            _jobService.EnqueueWeatherPreload(newTrip.TripId);
            return newTrip;
        }

        public async Task<(string InviteUrl, string QrCodeBase64)> GenerateShareLinkAsync(string frontendBaseUrl, Guid tripId, Guid userId)
        {
            var trip = await _tripRepo.GetByIdAsync(tripId);
            if (trip == null)
            {
                throw new Exception("Trip not found");
            }

            if (trip.OwnerId != userId)
            {
                throw new Exception("Only the owner can generate a share link");
            }

            if (string.IsNullOrEmpty(trip.ShareToken))
            {
                trip.ShareToken = Guid.NewGuid().ToString("N");
                await _tripRepo.UpdateAsync(trip);
            }

            var inviteUrl = $"{frontendBaseUrl.TrimEnd('/')}/{trip.ShareToken}";
            var qrCodeBase64 = _qrCodeGenerator.GenerateQRCodeBase64(inviteUrl);

            return (inviteUrl, qrCodeBase64);
        }

        public async Task JoinTripAsync(string token, Guid userId)
        {
            var trip = await _tripRepo.GetByShareTokenAsync(token);
            if (trip == null)
            {
                throw new Exception("Invalid invite link");
            }

            if (trip.OwnerId == userId)
            {
                return; // Owner is already part of the trip
            }

            var existingParticipant = await _participantRepo.GetByUserIdAndTripIdAsync(userId, trip.TripId);
            if (existingParticipant != null)
            {
                return; // User is already a participant
            }

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                TripId = trip.TripId,
                UserId = userId,
                Role = ParticipantRole.Viewer, // Giving viewer role by default
                Status = ParticipantStatus.Active
            };

            await _participantRepo.AddTripParticipantAsync(participant);
            await _participantRepo.SaveChangesAsync();
        }

        public async Task<ParticipantRole?> GetUserRoleInTripAsync(Guid tripId, Guid userId)
        {
            var trip = await _tripRepo.GetByIdAsync(tripId);
            if (trip == null)
            {
                throw new Exception("Trip not found");
            }

            if (trip.OwnerId == userId)
            {
                return ParticipantRole.Owner;
            }

            var participant = await _participantRepo.GetByUserIdAndTripIdAsync(userId, tripId);
            if (participant != null && participant.Status == ParticipantStatus.Active)
            {
                return participant.Role;
            }

            return null;
        }
    }
}

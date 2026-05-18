using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class PartnerProfileService : IPartnerProfileService
    {
        private readonly IPartnerProfileRepository _profileRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<PartnerProfileService> _logger;

        public PartnerProfileService(
            IPartnerProfileRepository profileRepo,
            ICloudinaryService cloudinaryService,
            ILogger<PartnerProfileService> logger)
        {
            _profileRepo = profileRepo;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<PartnerProfileResponse?> GetMyProfileAsync(Guid accountId)
        {
            var profile = await _profileRepo.GetByAccountIdAsync(accountId);
            if (profile == null)
                return null;

            return MapToResponse(profile);
        }

        public async Task<PartnerProfileResponse> UpdateMyProfileAsync(Guid accountId, UpdatePartnerProfileDto dto)
        {
            var profile = await _profileRepo.GetByAccountIdAsync(accountId);
            if (profile == null)
                throw new KeyNotFoundException("Hồ sơ Partner không tồn tại. Vui lòng liên hệ Admin.");

            if (dto.BusinessName != null)
                profile.BusinessName = dto.BusinessName;
            if (dto.BusinessAddress != null)
                profile.BusinessAddress = dto.BusinessAddress;
            if (dto.BusinessPhone != null)
                profile.BusinessPhone = dto.BusinessPhone;
            if (dto.BusinessEmail != null)
                profile.BusinessEmail = dto.BusinessEmail;
            profile.UpdatedAt = DateTime.UtcNow;

            var updated = await _profileRepo.UpdateAsync(profile);
            _logger.LogInformation("Partner profile updated for Account: {AccountId}", accountId);

            return MapToResponse(updated);
        }

        public async Task<PartnerProfileResponse> UpdateAvatarAsync(Guid accountId, Microsoft.AspNetCore.Http.IFormFile avatarFile)
        {
            var profile = await _profileRepo.GetByAccountIdAsync(accountId);
            if (profile == null)
                throw new KeyNotFoundException("Hồ sơ Partner không tồn tại. Vui lòng liên hệ Admin.");

            if (avatarFile == null || avatarFile.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ.");

            using var stream = avatarFile.OpenReadStream();
            var avatarUrl = await _cloudinaryService.UploadImageAsync(stream, avatarFile.FileName);

            profile.BusinessAvatarUrl = avatarUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            var updated = await _profileRepo.UpdateAsync(profile);
            _logger.LogInformation("Partner avatar updated for Account: {AccountId}", accountId);

            return MapToResponse(updated);
        }

        private static PartnerProfileResponse MapToResponse(PartnerProfile profile)
        {
            return new PartnerProfileResponse
            {
                Id = profile.Id,
                AccountId = profile.AccountId,
                BusinessName = profile.BusinessName,
                BusinessAddress = profile.BusinessAddress,
                BusinessPhone = profile.BusinessPhone,
                BusinessEmail = profile.BusinessEmail,
                BusinessLicenseUrl = profile.BusinessLicenseUrl,
                BusinessAvatarUrl = profile.BusinessAvatarUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}

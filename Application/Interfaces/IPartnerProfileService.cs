using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IPartnerProfileService
    {
        /// <summary>
        /// Partner lấy thông tin doanh nghiệp của mình
        /// </summary>
        Task<PartnerProfileResponse?> GetMyProfileAsync(Guid accountId);

        /// <summary>
        /// Partner cập nhật thông tin doanh nghiệp
        /// </summary>
        Task<PartnerProfileResponse> UpdateMyProfileAsync(Guid accountId, UpdatePartnerProfileDto dto);

        /// <summary>
        /// Partner cập nhật riêng Logo doanh nghiệp (upload file trực tiếp)
        /// </summary>
        Task<PartnerProfileResponse> UpdateAvatarAsync(Guid accountId, IFormFile avatarFile);
    }
}

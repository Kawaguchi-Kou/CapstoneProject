using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class PartnerRequestService : IPartnerRequestService
    {
        private readonly IPartnerRequestRepository _requestRepo;
        private readonly IPartnerProfileRepository _profileRepo;
        private readonly IAuthRepository _authRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly ILogger<PartnerRequestService> _logger;

        public PartnerRequestService(
            IPartnerRequestRepository requestRepo,
            IPartnerProfileRepository profileRepo,
            IAuthRepository authRepo,
            ICloudinaryService cloudinaryService,
            IUnitOfWork unitOfWork,
            IRealtimeNotifier realtimeNotifier,
            ILogger<PartnerRequestService> logger)
        {
            _requestRepo = requestRepo;
            _profileRepo = profileRepo;
            _authRepo = authRepo;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
            _realtimeNotifier = realtimeNotifier;
            _logger = logger;
        }

        public async Task<PartnerRequestResponse> CreateRequestAsync(Guid accountId, CreatePartnerRequestDto dto)
        {
            // Kiểm tra account tồn tại
            var account = await _authRepo.GetByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Account không tồn tại.");

            // Kiểm tra đã là Partner chưa
            if (account.Role?.Name == "Partner")
                throw new InvalidOperationException("Bạn đã là Partner rồi.");

            // Kiểm tra đã có đơn Pending chưa
            var hasPending = await _requestRepo.HasPendingRequestAsync(accountId);
            if (hasPending)
                throw new InvalidOperationException("Bạn đã có đơn đang chờ duyệt. Vui lòng đợi kết quả trước khi gửi đơn mới.");

            // Upload file giấy phép kinh doanh nếu có
            string businessLicenseUrl = string.Empty;
            if (dto.BusinessLicenseFile != null && dto.BusinessLicenseFile.Length > 0)
            {
                var contentType = dto.BusinessLicenseFile.ContentType.ToLower();
                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };
                
                var extension = Path.GetExtension(dto.BusinessLicenseFile.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

                if (!allowedImageTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
                {
                    throw new InvalidOperationException("Giấy phép kinh doanh phải là file ảnh (JPG, JPEG, PNG, GIF, WEBP, BMP). Không chấp nhận các định dạng file văn bản hoặc PDF.");
                }

                using var stream = dto.BusinessLicenseFile.OpenReadStream();
                businessLicenseUrl = await _cloudinaryService.UploadFileAsync(
                    stream,
                    dto.BusinessLicenseFile.FileName,
                    "image");
            }

            var request = new PartnerRequest
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                BusinessName = dto.BusinessName,
                BusinessAddress = dto.BusinessAddress,
                BusinessPhone = dto.BusinessPhone,
                BusinessEmail = dto.BusinessEmail,
                BusinessLicenseUrl = businessLicenseUrl,
                Status = PartnerRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _requestRepo.CreateAsync(request);
            _logger.LogInformation("Partner request created: {RequestId} by Account: {AccountId}", created.Id, accountId);

            var response = MapToResponse(created, account);

            try
            {
                await _realtimeNotifier.SendBroadcastNotificationAsync(new
                {
                    Type = "PARTNER_REQUEST_CREATED",
                    Request = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for Partner Request creation.");
            }

            return response;
        }

        public async Task<PartnerRequestResponse?> GetMyLatestRequestAsync(Guid accountId)
        {
            var request = await _requestRepo.GetLatestByAccountIdAsync(accountId);
            if (request == null)
                return null;

            var account = await _authRepo.GetByIdAsync(accountId);
            return MapToResponse(request, account);
        }

        public async Task<PagedResultResponse<PartnerRequestResponse>> GetPendingRequestsAsync(int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var items = await _requestRepo.GetByStatusAsync(PartnerRequestStatus.Pending, skip, pageSize);
            var totalItems = await _requestRepo.CountByStatusAsync(PartnerRequestStatus.Pending);

            return new PagedResultResponse<PartnerRequestResponse>
            {
                Items = items.Select(r => MapToResponse(r, r.Account)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<PartnerRequestResponse> ReviewRequestAsync(Guid requestId, Guid reviewerId, ReviewPartnerRequestDto dto)
        {
            var request = await _requestRepo.GetByIdAsync(requestId);
            if (request == null)
                throw new KeyNotFoundException("Đơn đăng ký không tồn tại.");

            if (request.Status != PartnerRequestStatus.Pending)
                throw new InvalidOperationException("Đơn này đã được xử lý trước đó.");

            request.AdminNote = dto.AdminNote;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedBy = reviewerId;

            if (dto.IsApproved)
            {
                await ApproveRequestAsync(request);
            }
            else
            {
                request.Status = PartnerRequestStatus.Rejected;
                await _requestRepo.UpdateAsync(request);
                _logger.LogInformation("Partner request rejected: {RequestId} by Reviewer: {ReviewerId}", requestId, reviewerId);
            }

            var response = MapToResponse(request, request.Account);

            try
            {
                await _realtimeNotifier.SendUserNotificationAsync(request.AccountId, new
                {
                    Type = "PARTNER_REQUEST_REVIEWED",
                    RequestId = request.Id,
                    Status = request.Status.ToString(),
                    AdminNote = request.AdminNote
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR user notification for Partner Request review.");
            }

            try
            {
                await _realtimeNotifier.SendBroadcastNotificationAsync(new
                {
                    Type = "PARTNER_REQUEST_REVIEWED",
                    RequestId = request.Id,
                    Status = request.Status.ToString(),
                    AdminNote = request.AdminNote
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR broadcast notification for Partner Request review.");
            }

            return response;
        }

        /// <summary>
        /// Logic duyệt đơn: thực hiện trong 1 Transaction
        /// 1. Cập nhật status đơn -> Approved
        /// 2. Đổi RoleId của Account -> Partner
        /// 3. Tạo PartnerProfile từ thông tin đơn
        /// </summary>
        private async Task ApproveRequestAsync(PartnerRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Cập nhật trạng thái đơn
                request.Status = PartnerRequestStatus.Approved;
                await _requestRepo.UpdateAsync(request);

                // 2. Đổi Role của Account thành Partner
                var account = await _authRepo.GetByIdAsync(request.AccountId);
                if (account == null)
                    throw new KeyNotFoundException("Account không tồn tại.");

                // Query RoleId của Partner từ bảng Roles trong DB
                var partnerRole = await _authRepo.GetRoleByNameAsync("Partner");
                if (partnerRole == null)
                    throw new InvalidOperationException("Role 'Partner' chưa được tạo trong hệ thống. Vui lòng liên hệ Admin.");

                account.RoleId = partnerRole.Id;
                await _authRepo.SaveChangesAsync();

                // 3. Tạo PartnerProfile từ thông tin đơn đăng ký
                var profile = new PartnerProfile
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.AccountId,
                    BusinessName = request.BusinessName,
                    BusinessAddress = request.BusinessAddress,
                    BusinessPhone = request.BusinessPhone,
                    BusinessEmail = request.BusinessEmail,
                    BusinessLicenseUrl = request.BusinessLicenseUrl,
                    BusinessAvatarUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
                await _profileRepo.CreateAsync(profile);

                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Partner request approved: {RequestId}. Account {AccountId} promoted to Partner.", request.Id, request.AccountId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static PartnerRequestResponse MapToResponse(PartnerRequest request, Account? account)
        {
            return new PartnerRequestResponse
            {
                Id = request.Id,
                AccountId = request.AccountId,
                AccountName = account?.Name ?? string.Empty,
                AccountEmail = account?.Email ?? string.Empty,
                BusinessName = request.BusinessName,
                BusinessAddress = request.BusinessAddress,
                BusinessPhone = request.BusinessPhone,
                BusinessEmail = request.BusinessEmail,
                BusinessLicenseUrl = request.BusinessLicenseUrl,
                Status = request.Status,
                AdminNote = request.AdminNote,
                CreatedAt = request.CreatedAt,
                ReviewedAt = request.ReviewedAt,
                ReviewedBy = request.ReviewedBy
            };
        }
    }
}

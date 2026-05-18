using Application.DTOs.Requests;
using Application.DTOs.Responses;

namespace Application.Interfaces
{
    public interface IPartnerRequestService
    {
        /// <summary>
        /// User gửi đơn đăng ký trở thành Partner
        /// </summary>
        Task<PartnerRequestResponse> CreateRequestAsync(Guid accountId, CreatePartnerRequestDto dto);

        /// <summary>
        /// User xem tình trạng đơn đăng ký mới nhất của mình
        /// </summary>
        Task<PartnerRequestResponse?> GetMyLatestRequestAsync(Guid accountId);

        /// <summary>
        /// Admin/Staff lấy danh sách đơn chờ duyệt (có phân trang)
        /// </summary>
        Task<PagedResultResponse<PartnerRequestResponse>> GetPendingRequestsAsync(int page, int pageSize);

        /// <summary>
        /// Admin/Staff duyệt hoặc từ chối đơn
        /// </summary>
        Task<PartnerRequestResponse> ReviewRequestAsync(Guid requestId, Guid reviewerId, ReviewPartnerRequestDto dto);
    }
}

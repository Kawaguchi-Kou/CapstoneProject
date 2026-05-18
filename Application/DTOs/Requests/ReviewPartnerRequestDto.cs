using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests
{
    public class ReviewPartnerRequestDto
    {
        /// <summary>
        /// true = Approve, false = Reject
        /// </summary>
        [Required]
        public bool IsApproved { get; set; }

        /// <summary>
        /// Ghi chú của Admin/Staff khi duyệt hoặc từ chối
        /// </summary>
        [MaxLength(1000)]
        public string AdminNote { get; set; } = string.Empty;
    }
}

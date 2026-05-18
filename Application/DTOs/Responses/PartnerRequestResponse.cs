using Domain.Enums;

namespace Application.DTOs.Responses
{
    public class PartnerRequestResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BusinessPhone { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string BusinessLicenseUrl { get; set; } = string.Empty;
        public PartnerRequestStatus Status { get; set; }
        public string AdminNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }
    }
}

namespace Application.DTOs.Responses
{
    public class PartnerProfileResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BusinessPhone { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string BusinessLicenseUrl { get; set; } = string.Empty;
        public string BusinessAvatarUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

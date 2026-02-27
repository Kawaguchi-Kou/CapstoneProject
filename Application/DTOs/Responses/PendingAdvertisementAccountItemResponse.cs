namespace Application.DTOs.Responses
{
    public class PendingAdvertisementAccountItemResponse
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int PendingAdsCount { get; set; }
        public DateTime LatestPendingAt { get; set; }
    }
}

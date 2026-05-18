namespace ArbiScannerAdminPanel.Domain.Models.DTOs
{
    public class UserSubscriptionRowDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserMail { get; set; } = string.Empty;
        public string SubcriptionType { get; set; } = string.Empty;
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}

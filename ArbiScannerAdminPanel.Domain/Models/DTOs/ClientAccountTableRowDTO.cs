namespace ArbiScannerAdminPanel.Domain.Models.DTOs
{
    public class ClientAccountTableRowDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserMail { get; set; } = string.Empty;
        public bool IsActiveSubscription { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}

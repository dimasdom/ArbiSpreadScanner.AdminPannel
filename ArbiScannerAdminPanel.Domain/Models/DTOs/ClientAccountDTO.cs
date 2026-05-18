namespace ArbiScannerAdminPanel.Domain.Models.DTOs
{
    public class ClientAccountDTO
    {
        public string Id { get; set; } = string.Empty;
        public string UserMail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserSubscriptionPayment? Subscription { get; set; }
        public List<PaymentModel>? Payments { get; set; }
    }
}

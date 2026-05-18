namespace ArbiScannerAdminPanel.Domain.Models
{
    public class SubscriptionModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = default!;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
    }
}

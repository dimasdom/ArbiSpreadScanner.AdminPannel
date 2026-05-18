namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class UserSubscriptionPaymentDTO
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int SubscriptionId { get; set; }

    public int PaymentId { get; set; }

    public DateTime ExpirationDate { get; set; }

    public string SubscriptionType { get; set; } = string.Empty;

    public decimal SubscriptionPrice { get; set; }

    public PaymentResultDTO? Payment { get; set; }
}

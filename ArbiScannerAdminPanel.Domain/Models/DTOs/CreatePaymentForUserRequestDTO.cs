namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class CreatePaymentForUserRequestDTO
{
    public string UserId { get; set; } = string.Empty;

    public int SubscriptionId { get; set; }
}

namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class UserSubscriptionCreateDTO
{
    public required string UserEmail { get; set; }
    public int SubscriptionId { get; set; }
}

namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class OxaPayPaymentStatusDTO
{
    public string TrackId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long ExpiredAt { get; set; }
    public long Date { get; set; }
    public string? OrderId { get; set; }
    public string? Description { get; set; }
    public PaymentStatus LocalStatus { get; set; } = PaymentStatus.Pending;
}

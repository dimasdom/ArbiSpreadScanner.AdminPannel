namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class OxaPayInvoiceResultDTO
{
    public int UserSubscriptionPaymentId { get; set; }
    public int PaymentId { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public long ExpiredAt { get; set; }
    public long Date { get; set; }
}

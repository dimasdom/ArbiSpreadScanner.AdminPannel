namespace ArbiScannerAdminPanel.Domain.Models.DTOs;

public class OxaPayInvoiceCreateOptionsDTO
{
    public string? Currency { get; set; }
    public int? Lifetime { get; set; }
    public decimal? FeePaidByPayer { get; set; }
    public decimal? UnderPaidCoverage { get; set; }
    public string? ToCurrency { get; set; }
    public bool? AutoWithdrawal { get; set; }
    public bool? MixedPayment { get; set; }
    public string? CallbackUrl { get; set; }
    public string? ReturnUrl { get; set; }
    public string? Email { get; set; }
    public string? OrderId { get; set; }
    public string? ThanksMessage { get; set; }
    public string? Description { get; set; }
    public bool? Sandbox { get; set; }
}

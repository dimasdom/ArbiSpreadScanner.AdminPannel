namespace ArbiScannerAdminPanel.Domain.Models;

public class OxaPaySettings
{
    public const string SectionName = "OxaPay";

    public string BaseUrl { get; set; } = string.Empty;

    public string MerchantApiKey { get; set; } = string.Empty;

    public string DefaultCurrency { get; set; } = "USD";

    public int DefaultLifetime { get; set; } = 30;

    public bool Sandbox { get; set; } = true;
}

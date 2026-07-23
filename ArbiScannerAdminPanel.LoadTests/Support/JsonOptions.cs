using System.Text.Json;

namespace ArbiScannerAdminPanel.LoadTests.Support;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

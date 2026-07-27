namespace ArbiScannerAdminPanel.IntegrationTests.Support;

internal static class JwtTestSettings
{
    public const string SigningKey = "integration-tests-signing-key-32-chars-minimum";
    public const string Issuer = "ArbiScannerAdminPanel.IntegrationTests";
    public const string Audience = "ArbiScannerAdminPanel.IntegrationTests";

    public static readonly Dictionary<string, string?> ConfigOverrides = new()
    {
        ["Jwt:SigningKey"] = SigningKey,
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
    };
}

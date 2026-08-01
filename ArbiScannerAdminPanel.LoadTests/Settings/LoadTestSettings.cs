namespace ArbiScannerAdminPanel.LoadTests.Settings;

public sealed class LoadTestSettings
{
    public required string BaseUrl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required int QueriesPerMinute { get; init; }
    public required TimeSpan Duration { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public static LoadTestSettings FromEnvironment()
    {
        var queriesPerMinute = ReadInt("ADMINPANEL_LOADTEST_QUERIES_PER_MINUTE", 60);
        var durationSeconds = ReadInt("ADMINPANEL_LOADTEST_DURATION_SECONDS", 60);

        return new LoadTestSettings
        {
            BaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_BASE_URL")),
            Username = Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_USERNAME") ?? string.Empty,
            Password = Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_PASSWORD") ?? string.Empty,
            QueriesPerMinute = queriesPerMinute,
            Duration = TimeSpan.FromSeconds(durationSeconds)
        };
    }

    // HttpClient.BaseAddress only appends relative request URIs when the base path
    // ends in '/' - without the trailing slash, combining drops the base's last
    // path segment (e.g. a gateway prefix like /adminapi) instead of keeping it.
    private static string NormalizeBaseUrl(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.TrimEnd('/') + "/";

    private static int ReadInt(string variable, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(variable);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }
}

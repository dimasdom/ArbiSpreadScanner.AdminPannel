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
            BaseUrl = (Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_BASE_URL") ?? string.Empty).TrimEnd('/'),
            Username = Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_USERNAME") ?? string.Empty,
            Password = Environment.GetEnvironmentVariable("ADMINPANEL_LOADTEST_PASSWORD") ?? string.Empty,
            QueriesPerMinute = queriesPerMinute,
            Duration = TimeSpan.FromSeconds(durationSeconds)
        };
    }

    private static int ReadInt(string variable, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(variable);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }
}

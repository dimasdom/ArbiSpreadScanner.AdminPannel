using ArbiScannerAdminPanel.LoadTests.Settings;
using ArbiScannerAdminPanel.LoadTests.Support;
using FluentAssertions;

namespace ArbiScannerAdminPanel.LoadTests.LoadTests;

public class SubscriptionsFetchLoadTest
{
    [SkippableFact]
    public async Task GetAllSubscriptions_UnderSustainedLoad_HasNoFailedRequests()
    {
        var settings = LoadTestSettings.FromEnvironment();
        Skip.IfNot(settings.IsConfigured, "ADMINPANEL_LOADTEST_BASE_URL / ADMINPANEL_LOADTEST_USERNAME / ADMINPANEL_LOADTEST_PASSWORD are not set.");

        var session = await AuthenticatedClientFactory.CreateAsync(settings);
        using var client = session.Client;

        var result = await LoadRunner.RunAsync(
            async () =>
            {
                var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");
                return response.IsSuccessStatusCode;
            },
            settings.QueriesPerMinute,
            settings.Duration);

        result.OkCount.Should().BeGreaterThan(0, "at least one request should have succeeded");
        result.FailCount.Should().Be(0, "no request against GetAllSubscriptions should fail under the configured load");
    }
}

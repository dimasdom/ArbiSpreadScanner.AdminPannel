using System.Net.Http.Json;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.LoadTests.Settings;
using ArbiScannerAdminPanel.LoadTests.Support;
using FluentAssertions;

namespace ArbiScannerAdminPanel.LoadTests.LoadTests;

public class SubscriptionUpdateLoadTest
{
    [SkippableFact]
    public async Task UpdateSubscription_UnderSustainedLoad_HasNoFailedRequests()
    {
        var settings = LoadTestSettings.FromEnvironment();
        Skip.IfNot(settings.IsConfigured, "ADMINPANEL_LOADTEST_BASE_URL / ADMINPANEL_LOADTEST_USERNAME / ADMINPANEL_LOADTEST_PASSWORD are not set.");

        var session = await AuthenticatedClientFactory.CreateAsync(settings);
        using var client = session.Client;

        Skip.IfNot(session.Account.Roles.Contains("Administrator"), "The configured load test account is not in the Administrator role required by UpdateSubscription.");

        var listResponse = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");
        listResponse.EnsureSuccessStatusCode();
        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<List<SubscriptionModel>>>(JsonOptions.CaseInsensitive);
        var subscription = listResult?.Value?.FirstOrDefault();
        Skip.If(subscription is null, "No subscriptions are seeded on the target environment.");

        var result = await LoadRunner.RunAsync(
            async () =>
            {
                var response = await client.PostAsJsonAsync("/api/Subscriptions/UpdateSubscription", subscription);
                return response.IsSuccessStatusCode;
            },
            settings.QueriesPerMinute,
            settings.Duration);

        result.OkCount.Should().BeGreaterThan(0, "at least one request should have succeeded");
        result.FailCount.Should().Be(0, "no request against UpdateSubscription should fail under the configured load");
    }
}

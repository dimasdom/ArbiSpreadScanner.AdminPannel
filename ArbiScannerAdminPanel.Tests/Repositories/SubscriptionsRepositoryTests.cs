using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;

namespace ArbiScannerAdminPanel.Tests.Repositories;

public class SubscriptionsRepositoryTests
{
    [Fact]
    public async Task GetSubscriptionById_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetSubscriptionById(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriptionById_Found_ReturnsSubscription()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetSubscriptionById(subscription.Id);

        result.Should().NotBeNull();
        result!.Type.Should().Be("Basic");
    }

    [Fact]
    public async Task GetAllSubscriptions_PagesResultsOrderedById()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        for (var i = 0; i < 25; i++)
        {
            context.Subscriptions.Add(new SubscriptionModel { Type = $"Type{i}", Price = i, DurationInDays = 30 });
        }
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var page1 = await sut.GetAllSubscriptions(1);
        var page2 = await sut.GetAllSubscriptions(2);

        page1.Should().HaveCount(20);
        page2.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllSubscriptions_NonPositivePage_NormalizesToFirstPage()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Subscriptions.Add(new SubscriptionModel { Type = "Basic", Price = 1, DurationInDays = 30 });
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetAllSubscriptions(-1);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task AddSubscription_TracksNewEntity()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        await sut.AddSubscription(new SubscriptionModel { Type = "Basic", Price = 5, DurationInDays = 10 });
        await sut.SaveChangesAsync();

        context.Subscriptions.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSubscriptionsByIds_EmptyList_ReturnsEmpty()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetSubscriptionsByIds(new List<int>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubscriptionsByIds_ReturnsOnlyMatchingIds()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Subscriptions.AddRange(
            new SubscriptionModel { Type = "A", Price = 1, DurationInDays = 30 },
            new SubscriptionModel { Type = "B", Price = 2, DurationInDays = 30 });
        await context.SaveChangesAsync();
        var ids = context.Subscriptions.Select(s => s.Id).Take(1).ToList();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetSubscriptionsByIds(ids);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveSubscriptions_RemovesFromContext()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscriptions = new List<SubscriptionModel> { new() { Type = "A", Price = 1, DurationInDays = 30 } };
        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        sut.RemoveSubscriptions(subscriptions);
        await sut.SaveChangesAsync();

        context.Subscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSubscriptionById_Found_IncludesSubscription()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        var userSubscription = new UserSubscriptionModel { UserId = "u1", SubscriptionId = subscription.Id, EndDate = DateTime.UtcNow.AddDays(30) };
        context.UserSubscriptions.Add(userSubscription);
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetUserSubscriptionById(userSubscription.Id);

        result.Should().NotBeNull();
        result!.Subscription.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionById_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetUserSubscriptionById(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestUserSubscriptionByUserId_ReturnsLatestByEndDate()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        context.UserSubscriptions.AddRange(
            new UserSubscriptionModel { UserId = "u1", SubscriptionId = subscription.Id, EndDate = DateTime.UtcNow.AddDays(5) },
            new UserSubscriptionModel { UserId = "u1", SubscriptionId = subscription.Id, EndDate = DateTime.UtcNow.AddDays(30) });
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetLatestUserSubscriptionByUserId("u1");

        result.Should().NotBeNull();
        result!.EndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAllUserSubscriptionsWithSubscription_PagesResultsOrderedByStartDateDescending()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        for (var i = 0; i < 3; i++)
        {
            context.UserSubscriptions.Add(new UserSubscriptionModel
            {
                UserId = $"u{i}",
                SubscriptionId = subscription.Id,
                StartDate = DateTime.UtcNow.AddMinutes(i),
                EndDate = DateTime.UtcNow.AddDays(30)
            });
        }
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetAllUserSubscriptionsWithSubscription(1);

        result.Should().HaveCount(3);
        result[0].UserId.Should().Be("u2");
    }

    [Fact]
    public async Task GetUserSubscriptionsByIds_EmptyList_ReturnsEmpty()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetUserSubscriptionsByIds(new List<int>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSubscriptionsByIds_ReturnsOnlyMatchingIds()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.UserSubscriptions.AddRange(
            new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(1) },
            new UserSubscriptionModel { UserId = "u2", EndDate = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        var ids = context.UserSubscriptions.Select(s => s.Id).Take(1).ToList();
        var sut = new SubscriptionsRepository(context);

        var result = await sut.GetUserSubscriptionsByIds(ids);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task AddUserSubscription_TracksNewEntity()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new SubscriptionsRepository(context);

        await sut.AddUserSubscription(new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(1) });
        await sut.SaveChangesAsync();

        context.UserSubscriptions.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveUserSubscriptions_RemovesFromContext()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subs = new List<UserSubscriptionModel> { new() { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(1) } };
        context.UserSubscriptions.AddRange(subs);
        await context.SaveChangesAsync();
        var sut = new SubscriptionsRepository(context);

        sut.RemoveUserSubscriptions(subs);
        await sut.SaveChangesAsync();

        context.UserSubscriptions.Should().BeEmpty();
    }
}

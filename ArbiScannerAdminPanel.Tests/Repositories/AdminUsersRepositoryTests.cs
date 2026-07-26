using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;

namespace ArbiScannerAdminPanel.Tests.Repositories;

public class AdminUsersRepositoryTests
{
    [Fact]
    public async Task GetUserSubscriptionPaymentByUserId_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new AdminUsersRepository(context);

        var result = await sut.GetUserSubscriptionPaymentByUserId("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionPaymentByUserId_Found_IncludesSubscriptionAndPayment()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        var payment = new PaymentModel { UserId = "u1", Amount = 10 };
        context.Subscriptions.Add(subscription);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var sut = new AdminUsersRepository(context);

        var result = await sut.GetUserSubscriptionPaymentByUserId("u1");

        result.Should().NotBeNull();
        result!.Subscription.Should().NotBeNull();
        result.Payment.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentsByUserId_ReturnsOnlyMatchingUser()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Payments.AddRange(
            new PaymentModel { UserId = "u1", Amount = 5 },
            new PaymentModel { UserId = "u1", Amount = 15 },
            new PaymentModel { UserId = "u2", Amount = 25 });
        await context.SaveChangesAsync();
        var sut = new AdminUsersRepository(context);

        var result = await sut.GetPaymentsByUserId("u1");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.UserId == "u1");
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_ReturnsLatestByEndDate()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.UserSubscriptions.AddRange(
            new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(5) },
            new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(30) },
            new UserSubscriptionModel { UserId = "u2", EndDate = DateTime.UtcNow.AddDays(60) });
        await context.SaveChangesAsync();
        var sut = new AdminUsersRepository(context);

        var result = await sut.GetUserSubscriptionByUserId("u1");

        result.Should().NotBeNull();
        result!.EndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }
}

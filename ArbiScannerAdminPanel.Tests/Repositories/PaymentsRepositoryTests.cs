using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;

namespace ArbiScannerAdminPanel.Tests.Repositories;

public class PaymentsRepositoryTests
{
    private static async Task<(SubscriptionModel Subscription, PaymentModel Payment)> SeedSubscriptionAndPayment(
        Infrastructure.DbContext.AdminPanelAppDbContext context, string userId = "u1", string transactionId = "TRK1", PaymentStatus status = PaymentStatus.Pending)
    {
        var subscription = new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 };
        var payment = new PaymentModel { UserId = userId, Amount = 10, TransactionId = transactionId, Status = status, PaymentDate = DateTime.UtcNow };
        context.Subscriptions.Add(subscription);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        return (subscription, payment);
    }

    [Fact]
    public async Task GetUserSubscriptionPaymentByTransactionId_Found_IncludesPayment()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context);
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetUserSubscriptionPaymentByTransactionId("TRK1");

        result.Should().NotBeNull();
        result!.Payment!.TransactionId.Should().Be("TRK1");
    }

    [Fact]
    public async Task GetUserSubscriptionPaymentByTransactionId_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetUserSubscriptionPaymentByTransactionId("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionPaymentByPaymentId_Found_ReturnsMatch()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context);
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetUserSubscriptionPaymentByPaymentId(payment.Id);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionPaymentWithDetails_Found_IncludesSubscriptionAndPayment()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context);
        var usp = new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        };
        context.UserSubscriptionPayments.Add(usp);
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetUserSubscriptionPaymentWithDetails(usp.Id);

        result.Should().NotBeNull();
        result!.Subscription.Should().NotBeNull();
        result.Payment.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveUserPayment_ReturnsPendingUnexpiredPayment()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context, status: PaymentStatus.Pending);
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddMinutes(30)
        });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetActiveUserPayment("u1", DateTime.UtcNow);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveUserPayment_ExpiredPayment_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context, status: PaymentStatus.Pending);
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddMinutes(-30)
        });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetActiveUserPayment("u1", DateTime.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentsForUser_ReturnsOnlyMatchingUser()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var (subscription, payment) = await SeedSubscriptionAndPayment(context, userId: "u1");
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u1",
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        context.UserSubscriptionPayments.Add(new UserSubscriptionPayment
        {
            UserId = "u2",
            SubscriptionId = subscription.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetPaymentsForUser("u1");

        result.Should().ContainSingle();
        result.Single().UserId.Should().Be("u1");
    }

    [Fact]
    public async Task GetAllPayments_PagesResultsOrderedByDateDescending()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        for (var i = 0; i < 25; i++)
        {
            context.Payments.Add(new PaymentModel { UserId = "u1", Amount = i, PaymentDate = DateTime.UtcNow.AddMinutes(i) });
        }
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var page1 = await sut.GetAllPayments(1);
        var page2 = await sut.GetAllPayments(2);

        page1.Should().HaveCount(20);
        page2.Should().HaveCount(5);
        page1.First().Amount.Should().Be(24);
    }

    [Fact]
    public async Task GetAllPayments_NonPositivePage_NormalizesToFirstPage()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Payments.Add(new PaymentModel { UserId = "u1", Amount = 1, PaymentDate = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetAllPayments(0);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPaymentsByIds_EmptyList_ReturnsEmpty()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetPaymentsByIds(new List<int>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentsByIds_ReturnsOnlyMatchingIds()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Payments.AddRange(
            new PaymentModel { UserId = "u1", Amount = 1 },
            new PaymentModel { UserId = "u1", Amount = 2 },
            new PaymentModel { UserId = "u1", Amount = 3 });
        await context.SaveChangesAsync();
        var ids = context.Payments.Take(2).Select(p => p.Id).ToList();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetPaymentsByIds(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPaymentById_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new PaymentsRepository(context);

        var result = await sut.GetPaymentById(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddUserSubscriptionPayment_TracksNewEntity()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new PaymentsRepository(context);
        var usp = new UserSubscriptionPayment { UserId = "u1", ExpirationDate = DateTime.UtcNow.AddDays(1) };

        await sut.AddUserSubscriptionPayment(usp);
        await sut.SaveChangesAsync();

        context.UserSubscriptionPayments.Should().ContainSingle();
    }

    [Fact]
    public async Task RemovePayment_RemovesFromContext()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var payment = new PaymentModel { UserId = "u1", Amount = 1 };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        sut.RemovePayment(payment);
        await sut.SaveChangesAsync();

        context.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task RemovePayments_RemovesAllFromContext()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var payments = new List<PaymentModel> { new() { UserId = "u1", Amount = 1 }, new() { UserId = "u1", Amount = 2 } };
        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
        var sut = new PaymentsRepository(context);

        sut.RemovePayments(payments);
        await sut.SaveChangesAsync();

        context.Payments.Should().BeEmpty();
    }
}

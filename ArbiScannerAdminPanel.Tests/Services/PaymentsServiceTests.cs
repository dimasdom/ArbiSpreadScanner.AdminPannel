using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class PaymentsServiceTests
{
    private readonly Mock<IPaymentsRepository> _paymentsRepository = new();
    private readonly Mock<ISubscriptionsRepository> _subscriptionsRepository = new();
    private readonly Mock<IWebAppUserRepository> _webAppUserRepository = new();
    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly Mock<IOxaPayService> _oxaPayService = new();
    private readonly PaymentsService _sut;

    public PaymentsServiceTests()
    {
        _sut = new PaymentsService(
            _paymentsRepository.Object,
            _subscriptionsRepository.Object,
            _webAppUserRepository.Object,
            _subscriptionService.Object,
            _oxaPayService.Object,
            NullLogger<PaymentsService>.Instance);
    }

    private static UserSubscriptionPayment CreatePayment(int id = 1, PaymentStatus status = PaymentStatus.Pending, string transactionId = "TRK1") => new()
    {
        Id = id,
        UserId = "user-1",
        SubscriptionId = 1,
        Payment = new PaymentModel { Id = id, Status = status, TransactionId = transactionId, Amount = 10m }
    };

    [Fact]
    public async Task AcceptPayment_NotFoundByTransactionOrId_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("unknown", true)).ReturnsAsync((UserSubscriptionPayment?)null);

        var result = await _sut.AcceptPayment("unknown");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptPayment_AlreadyCompleted_ReturnsOkWithoutReassigning()
    {
        var payment = CreatePayment(status: PaymentStatus.Completed);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);

        var result = await _sut.AcceptPayment("TRK1");

        result.IsSuccess.Should().BeTrue();
        _subscriptionService.Verify(s => s.AssignSubscriptionToUser(It.IsAny<UserSubscriptionPayment>()), Times.Never);
    }

    [Fact]
    public async Task AcceptPayment_Success_MarksCompletedAndAssignsSubscription()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(payment)).ReturnsAsync(Result.Ok(new UserSubscriptionModel()));

        var result = await _sut.AcceptPayment("TRK1");

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.Status.Should().Be(PaymentStatus.Completed);
        _paymentsRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AcceptPayment_SubscriptionAssignmentFails_ReturnsFail()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(payment)).ReturnsAsync(Result.Fail<UserSubscriptionModel>("boom"));

        var result = await _sut.AcceptPayment("TRK1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CancelPayment_NotFound_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync((UserSubscriptionPayment?)null);

        var result = await _sut.CancelPayment(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CancelPayment_Success_SetsRefunded()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);

        var result = await _sut.CancelPayment(1);

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task GenerateInvoice_PaymentNotFound_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync((UserSubscriptionPayment?)null);

        var result = await _sut.GenerateInvoice(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_AlreadyCompleted_ReturnsConflict()
    {
        var payment = CreatePayment(status: PaymentStatus.Completed);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);

        var result = await _sut.GenerateInvoice(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_UsesOptionsEmail_SkipsUserLookup()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);
        _oxaPayService.Setup(o => o.GenerateInvoice(payment, "options@test.com", It.IsAny<OxaPayInvoiceCreateOptionsDTO>()))
            .ReturnsAsync(Result.Ok(new OxaPayInvoiceResultDTO { TrackId = "TRK2", PaymentUrl = "url", ExpiredAt = 1999999999 }));

        var result = await _sut.GenerateInvoice(1, new OxaPayInvoiceCreateOptionsDTO { Email = "options@test.com" });

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.GetById(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateInvoice_FallsBackToUserEmail_WhenOptionsEmailMissing()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync(new WebAppUserDTO { Id = "user-1", Email = "fallback@test.com" });
        _oxaPayService.Setup(o => o.GenerateInvoice(payment, "fallback@test.com", null))
            .ReturnsAsync(Result.Ok(new OxaPayInvoiceResultDTO { TrackId = "TRK2", PaymentUrl = "url", ExpiredAt = 1999999999 }));

        var result = await _sut.GenerateInvoice(1);

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.GetById("user-1"), Times.Once);
    }

    [Fact]
    public async Task GenerateInvoice_OxaPayFails_ReturnsFail()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync((WebAppUserDTO?)null);
        _oxaPayService.Setup(o => o.GenerateInvoice(payment, null, null))
            .ReturnsAsync(Result.Fail<OxaPayInvoiceResultDTO>("oxapay down"));

        var result = await _sut.GenerateInvoice(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_Success_UpdatesPaymentFields()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(1, true)).ReturnsAsync(payment);
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync((WebAppUserDTO?)null);
        var invoice = new OxaPayInvoiceResultDTO { TrackId = "TRK2", PaymentUrl = "https://pay/TRK2", ExpiredAt = 1999999999 };
        _oxaPayService.Setup(o => o.GenerateInvoice(payment, null, null)).ReturnsAsync(Result.Ok(invoice));

        var result = await _sut.GenerateInvoice(1);

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.TransactionId.Should().Be("TRK2");
        payment.Payment.PaymentUrl.Should().Be("https://pay/TRK2");
        _paymentsRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentForUser_SubscriptionNotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync((SubscriptionModel?)null);

        var result = await _sut.CreatePaymentForUser(new UserSubscriptionPayment { UserId = "user-1", SubscriptionId = 1 });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePaymentForUser_ActivePaymentExists_ReturnsExistingWithoutCreatingNew()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync(new SubscriptionModel { Id = 1, Price = 10, DurationInDays = 30 });
        var activePayment = CreatePayment(id: 99, transactionId: string.Empty);
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync(activePayment);

        var result = await _sut.CreatePaymentForUser(new UserSubscriptionPayment { UserId = "user-1", SubscriptionId = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(99);
        _paymentsRepository.Verify(r => r.AddUserSubscriptionPayment(It.IsAny<UserSubscriptionPayment>()), Times.Never);
    }

    [Fact]
    public async Task CreatePaymentForUser_NoActivePayment_CreatesNewAndGeneratesInvoice()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync(new SubscriptionModel { Id = 1, Price = 10, DurationInDays = 30 });
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync((UserSubscriptionPayment?)null);

        UserSubscriptionPayment? added = null;
        _paymentsRepository.Setup(r => r.AddUserSubscriptionPayment(It.IsAny<UserSubscriptionPayment>()))
            .Callback<UserSubscriptionPayment>(p => added = p)
            .Returns(Task.CompletedTask);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentWithDetails(0, true))
            .ReturnsAsync(() => added);
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync((WebAppUserDTO?)null);
        var invoice = new OxaPayInvoiceResultDTO { TrackId = "TRK3", PaymentUrl = "https://pay/TRK3", ExpiredAt = 1999999999 };
        _oxaPayService.Setup(o => o.GenerateInvoice(It.IsAny<UserSubscriptionPayment>(), null, null)).ReturnsAsync(Result.Ok(invoice));

        var result = await _sut.CreatePaymentForUser(new UserSubscriptionPayment { UserId = "user-1", SubscriptionId = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Payment!.Amount.Should().Be(10);
        _paymentsRepository.Verify(r => r.AddUserSubscriptionPayment(It.IsAny<UserSubscriptionPayment>()), Times.Once);
    }

    [Fact]
    public async Task GetActivePaymentForUser_NoActivePayment_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync((UserSubscriptionPayment?)null);

        var result = await _sut.GetActivePaymentForUser("user-1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivePaymentForUser_NoTransactionId_ReturnsOkWithoutStatusCheck()
    {
        var payment = CreatePayment(transactionId: string.Empty);
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync(payment);

        var result = await _sut.GetActivePaymentForUser("user-1");

        result.IsSuccess.Should().BeTrue();
        _oxaPayService.Verify(o => o.GetInvoiceStatus(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetActivePaymentForUser_StatusCompleted_AcceptsPayment()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync(payment);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);
        _oxaPayService.Setup(o => o.GetInvoiceStatus("TRK1"))
            .ReturnsAsync(Result.Ok(new OxaPayPaymentStatusDTO { TrackId = "TRK1", LocalStatus = PaymentStatus.Completed }));
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(payment)).ReturnsAsync(Result.Ok(new UserSubscriptionModel()));

        var result = await _sut.GetActivePaymentForUser("user-1");

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task GetActivePaymentForUser_StatusFailed_UpdatesLocalStatus()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetActiveUserPayment("user-1", It.IsAny<DateTime>(), true)).ReturnsAsync(payment);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync((UserSubscriptionPayment?)null);
        _oxaPayService.Setup(o => o.GetInvoiceStatus("TRK1"))
            .ReturnsAsync(Result.Ok(new OxaPayPaymentStatusDTO { TrackId = "TRK1", LocalStatus = PaymentStatus.Failed }));

        var result = await _sut.GetActivePaymentForUser("user-1");

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task GetAllPayments_DelegatesToRepository()
    {
        _paymentsRepository.Setup(r => r.GetAllPayments(2, 20)).ReturnsAsync(new List<PaymentModel> { new() { Id = 1 } });

        var result = await _sut.GetAllPayments(2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPaymentById_NotFound_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetPaymentById(1)).ReturnsAsync((PaymentModel?)null);

        var result = await _sut.GetPaymentById(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentDTOById_UserNotFound_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetPaymentById(1)).ReturnsAsync(new PaymentModel { Id = 1, UserId = "user-1" });
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync((WebAppUserDTO?)null);

        var result = await _sut.GetPaymentDTOById(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentDTOById_Success_MapsDto()
    {
        _paymentsRepository.Setup(r => r.GetPaymentById(1)).ReturnsAsync(new PaymentModel { Id = 1, UserId = "user-1", Amount = 5m });
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync(new WebAppUserDTO { Id = "user-1", Email = "a@b.com" });

        var result = await _sut.GetPaymentDTOById(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserEmail.Should().Be("a@b.com");
    }

    [Fact]
    public async Task GetPaymentsForUser_UserNotFound_ReturnsFail()
    {
        _webAppUserRepository.Setup(w => w.GetById("user-1")).ReturnsAsync((WebAppUserDTO?)null);

        var result = await _sut.GetPaymentsForUser("user-1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task RemovePayment_NotFound_ReturnsFail()
    {
        _paymentsRepository.Setup(r => r.GetPaymentById(1)).ReturnsAsync((PaymentModel?)null);

        var result = await _sut.RemovePayment(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task RemovePayment_Success_RemovesAndSaves()
    {
        var payment = new PaymentModel { Id = 1 };
        _paymentsRepository.Setup(r => r.GetPaymentById(1)).ReturnsAsync(payment);

        var result = await _sut.RemovePayment(1);

        result.IsSuccess.Should().BeTrue();
        _paymentsRepository.Verify(r => r.RemovePayment(payment), Times.Once);
        _paymentsRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetInvoiceStatus_EmptyTrackId_ReturnsValidationFail()
    {
        var result = await _sut.GetInvoiceStatus(string.Empty);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvoiceStatus_OxaPayFails_ReturnsFail()
    {
        _oxaPayService.Setup(o => o.GetInvoiceStatus("TRK1")).ReturnsAsync(Result.Fail<OxaPayPaymentStatusDTO>("down"));

        var result = await _sut.GetInvoiceStatus("TRK1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvoiceStatus_CompletedLocally_AssignsSubscriptionAndUpdatesStatus()
    {
        var localPayment = CreatePayment();
        _oxaPayService.Setup(o => o.GetInvoiceStatus("TRK1"))
            .ReturnsAsync(Result.Ok(new OxaPayPaymentStatusDTO { TrackId = "TRK1", LocalStatus = PaymentStatus.Completed }));
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(localPayment);
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(localPayment)).ReturnsAsync(Result.Ok(new UserSubscriptionModel()));

        var result = await _sut.GetInvoiceStatus("TRK1");

        result.IsSuccess.Should().BeTrue();
        result.Value.LocalStatus.Should().Be(PaymentStatus.Completed);
        _subscriptionService.Verify(s => s.AssignSubscriptionToUser(localPayment), Times.Once);
    }

    [Fact]
    public async Task GetInvoiceStatus_SubscriptionAssignmentFails_ReturnsFail()
    {
        var localPayment = CreatePayment();
        _oxaPayService.Setup(o => o.GetInvoiceStatus("TRK1"))
            .ReturnsAsync(Result.Ok(new OxaPayPaymentStatusDTO { TrackId = "TRK1", LocalStatus = PaymentStatus.Completed }));
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(localPayment);
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(localPayment)).ReturnsAsync(Result.Fail<UserSubscriptionModel>("boom"));

        var result = await _sut.GetInvoiceStatus("TRK1");

        result.IsFailed.Should().BeTrue();
    }

    private static OxaPayWebhookPayloadDTO CreateWebhookPayload(
        string status = "Paid", string type = "invoice", string trackId = "TRK1", long? date = null) => new()
    {
        TrackId = trackId,
        Status = status,
        Type = type,
        Date = date ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    [Fact]
    public async Task HandleOxaPayWebhookAsync_PaidInvoice_AcceptsPayment()
    {
        var payment = CreatePayment();
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);
        _subscriptionService.Setup(s => s.AssignSubscriptionToUser(payment)).ReturnsAsync(Result.Ok(new UserSubscriptionModel()));

        var result = await _sut.HandleOxaPayWebhookAsync(CreateWebhookPayload());

        result.IsSuccess.Should().BeTrue();
        payment.Payment!.Status.Should().Be(PaymentStatus.Completed);
        _subscriptionService.Verify(s => s.AssignSubscriptionToUser(payment), Times.Once);
    }

    [Fact]
    public async Task HandleOxaPayWebhookAsync_AlreadyCompleted_DoesNotReassignSubscription()
    {
        var payment = CreatePayment(status: PaymentStatus.Completed);
        _paymentsRepository.Setup(r => r.GetUserSubscriptionPaymentByTransactionId("TRK1", true)).ReturnsAsync(payment);

        var result = await _sut.HandleOxaPayWebhookAsync(CreateWebhookPayload());

        result.IsSuccess.Should().BeTrue();
        _subscriptionService.Verify(s => s.AssignSubscriptionToUser(It.IsAny<UserSubscriptionPayment>()), Times.Never);
    }

    [Fact]
    public async Task HandleOxaPayWebhookAsync_NonInvoiceType_IsIgnored()
    {
        var result = await _sut.HandleOxaPayWebhookAsync(CreateWebhookPayload(type: "payout"));

        result.IsSuccess.Should().BeTrue();
        _paymentsRepository.Verify(r => r.GetUserSubscriptionPaymentByTransactionId(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleOxaPayWebhookAsync_NonPaidStatus_IsIgnored()
    {
        var result = await _sut.HandleOxaPayWebhookAsync(CreateWebhookPayload(status: "Paying"));

        result.IsSuccess.Should().BeTrue();
        _paymentsRepository.Verify(r => r.GetUserSubscriptionPaymentByTransactionId(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task HandleOxaPayWebhookAsync_StaleEvent_IsIgnored()
    {
        var staleDate = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();

        var result = await _sut.HandleOxaPayWebhookAsync(CreateWebhookPayload(date: staleDate));

        result.IsSuccess.Should().BeTrue();
        _paymentsRepository.Verify(r => r.GetUserSubscriptionPaymentByTransactionId(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }
}

using System.Text;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.API.Controllers;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentsService> _paymentsService = new();
    private readonly Mock<IOxaPayService> _oxaPayService = new();
    private readonly PaymentsController _sut;
    private readonly DefaultHttpContext _httpContext = new();

    public PaymentsControllerTests()
    {
        _sut = new PaymentsController(_paymentsService.Object, _oxaPayService.Object, NullLogger<PaymentsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private void SetRequestBody(string json, string? hmac = "valid-hmac")
    {
        _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        _httpContext.Request.ContentLength = Encoding.UTF8.GetByteCount(json);
        if (hmac != null)
        {
            _httpContext.Request.Headers["HMAC"] = hmac;
        }
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsUnauthorized()
    {
        SetRequestBody("{}");
        _oxaPayService.Setup(o => o.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string?>())).Returns(false);

        var result = await _sut.Webhook();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Webhook_MalformedJson_ReturnsBadRequest()
    {
        SetRequestBody("not-json");
        _oxaPayService.Setup(o => o.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);

        var result = await _sut.Webhook();

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Webhook_ValidPayload_HandlesAndReturnsOk()
    {
        SetRequestBody("""{"track_id":"TRK1","status":"Paid","type":"invoice","date":123}""");
        _oxaPayService.Setup(o => o.VerifyWebhookSignature(It.IsAny<string>(), "valid-hmac")).Returns(true);

        var result = await _sut.Webhook();

        result.Should().BeOfType<OkResult>();
        _paymentsService.Verify(p => p.HandleOxaPayWebhookAsync(It.Is<OxaPayWebhookPayloadDTO>(d => d.TrackId == "TRK1")), Times.Once);
    }

    [Fact]
    public async Task GetAllPayments_DelegatesToService()
    {
        _paymentsService.Setup(p => p.GetAllPayments(2)).ReturnsAsync(Result.Ok(new List<PaymentModel> { new() { Id = 1 } }));

        var actionResult = await _sut.GetAllPayments(2);
        var response = (SerializableResult<List<PaymentModel>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPaymentById_DelegatesToService()
    {
        _paymentsService.Setup(p => p.GetPaymentDTOById(5)).ReturnsAsync(Result.Ok(new PaymentResultDTO { Id = 5 }));

        var actionResult = await _sut.GetPaymentById(5);
        var response = (SerializableResult<PaymentResultDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.Id.Should().Be(5);
    }

    [Fact]
    public async Task RemovePayment_DelegatesToService()
    {
        _paymentsService.Setup(p => p.RemovePayments(It.IsAny<List<int>>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.RemovePayment(new List<int> { 1, 2 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentsForUser_Failure_ReturnsFail()
    {
        _paymentsService.Setup(p => p.GetPaymentsForUser("u1")).ReturnsAsync(Result.Fail<List<UserSubscriptionPayment>>("not found"));

        var actionResult = await _sut.GetPaymentsForUser("u1");
        var response = (SerializableResult<List<UserSubscriptionPaymentDTO>>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaymentsForUser_Success_MapsPayments()
    {
        var payment = new UserSubscriptionPayment
        {
            Id = 1,
            UserId = "u1",
            SubscriptionId = 1,
            Subscription = new SubscriptionModel { Type = "Basic", Price = 10 },
            Payment = new PaymentModel { Id = 1, UserId = "u1", Amount = 10, Status = PaymentStatus.Pending, TransactionId = "TRK1" }
        };
        _paymentsService.Setup(p => p.GetPaymentsForUser("u1")).ReturnsAsync(Result.Ok(new List<UserSubscriptionPayment> { payment }));

        var actionResult = await _sut.GetPaymentsForUser("u1");
        var response = (SerializableResult<List<UserSubscriptionPaymentDTO>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().ContainSingle();
        response.Value![0].Payment!.TransactionId.Should().Be("TRK1");
        response.Value![0].SubscriptionType.Should().Be("Basic");
    }

    [Fact]
    public async Task CreatePaymentForUser_Failure_ReturnsFail()
    {
        _paymentsService.Setup(p => p.CreatePaymentForUser(It.IsAny<UserSubscriptionPayment>()))
            .ReturnsAsync(Result.Fail<UserSubscriptionPayment>("no subscription"));

        var actionResult = await _sut.CreatePaymentForUser(new CreatePaymentForUserRequestDTO { UserId = "u1", SubscriptionId = 1 });
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePaymentForUser_Success_MapsPayment()
    {
        var created = new UserSubscriptionPayment { Id = 2, UserId = "u1", SubscriptionId = 1 };
        _paymentsService.Setup(p => p.CreatePaymentForUser(It.Is<UserSubscriptionPayment>(u => u.UserId == "u1" && u.SubscriptionId == 1)))
            .ReturnsAsync(Result.Ok(created));

        var actionResult = await _sut.CreatePaymentForUser(new CreatePaymentForUserRequestDTO { UserId = "u1", SubscriptionId = 1 });
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetActivePaymentForUser_Failure_ReturnsFail()
    {
        _paymentsService.Setup(p => p.GetActivePaymentForUser("u1")).ReturnsAsync(Result.Fail<UserSubscriptionPayment>("none"));

        var actionResult = await _sut.GetActivePaymentForUser("u1");
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetActivePaymentForUser_Success_MapsPayment()
    {
        var payment = new UserSubscriptionPayment { Id = 3, UserId = "u1" };
        _paymentsService.Setup(p => p.GetActivePaymentForUser("u1")).ReturnsAsync(Result.Ok(payment));

        var actionResult = await _sut.GetActivePaymentForUser("u1");
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.Id.Should().Be(3);
    }

    [Fact]
    public async Task GetUserPaymentByIdAsync_Failure_ReturnsFail()
    {
        _paymentsService.Setup(p => p.GetUserPaymentByIdAsync(9)).ReturnsAsync(Result.Fail<UserSubscriptionPayment>("none"));

        var actionResult = await _sut.GetUserPaymentByIdAsync(9);
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPaymentByIdAsync_Success_MapsPayment()
    {
        var payment = new UserSubscriptionPayment { Id = 9, UserId = "u1" };
        _paymentsService.Setup(p => p.GetUserPaymentByIdAsync(9)).ReturnsAsync(Result.Ok(payment));

        var actionResult = await _sut.GetUserPaymentByIdAsync(9);
        var response = (SerializableResult<UserSubscriptionPaymentDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.Id.Should().Be(9);
    }

    [Fact]
    public async Task CancelPayment_DelegatesToService()
    {
        _paymentsService.Setup(p => p.CancelPayment(4)).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.CancelPayment(4);
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }
}

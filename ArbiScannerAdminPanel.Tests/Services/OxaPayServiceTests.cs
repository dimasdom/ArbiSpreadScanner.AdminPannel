using System.Net;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class OxaPayServiceTests
{
    private static IConfiguration BuildConfiguration(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["OxaPay:MerchantApiKey"] = "merchant-key",
            ["OxaPay:BaseUrl"] = "https://api.oxapay.test",
            ["OxaPay:DefaultCurrency"] = "USD",
            ["OxaPay:DefaultLifetime"] = "30",
            ["OxaPay:Sandbox"] = "true"
        };
        if (overrides != null)
        {
            foreach (var kv in overrides)
                values[kv.Key] = kv.Value;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static (OxaPayService Service, Mock<IHttpClientFactory> Factory) CreateService(
        HttpStatusCode statusCode, string content, IConfiguration? configuration = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, content);
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new OxaPayService(factory.Object, configuration ?? BuildConfiguration(), NullLogger<OxaPayService>.Instance);
        return (service, factory);
    }

    private static UserSubscriptionPayment CreateUserPayment(PaymentModel? payment = null) => new()
    {
        Id = 1,
        UserId = "user-1",
        PaymentId = payment?.Id ?? 5,
        Payment = payment ?? new PaymentModel { Id = 5, Amount = 9.99m }
    };

    [Fact]
    public async Task GenerateInvoice_PaymentModelMissing_ReturnsFail()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, "{}");
        var userPayment = CreateUserPayment();
        userPayment.Payment = null;

        var result = await service.GenerateInvoice(userPayment, "user@test.com");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_MissingMerchantApiKey_ReturnsFail()
    {
        var config = BuildConfiguration(new Dictionary<string, string?> { ["OxaPay:MerchantApiKey"] = "" });
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", config);

        var result = await service.GenerateInvoice(CreateUserPayment(), "user@test.com");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_HttpErrorResponse_ReturnsFail()
    {
        var errorJson = """{"data":null,"message":"invalid amount","status":400}""";
        var (service, _) = CreateService(HttpStatusCode.BadRequest, errorJson);

        var result = await service.GenerateInvoice(CreateUserPayment(), "user@test.com");

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("invalid amount");
    }

    [Fact]
    public async Task GenerateInvoice_Success_ReturnsInvoiceResult()
    {
        var successJson = """
        {"data":{"track_id":"TRK1","payment_url":"https://pay.oxapay.test/TRK1","expired_at":1999999999,"date":1999999000},"message":"ok","status":200}
        """;
        var (service, _) = CreateService(HttpStatusCode.OK, successJson);

        var result = await service.GenerateInvoice(CreateUserPayment(), "user@test.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.TrackId.Should().Be("TRK1");
        result.Value.PaymentUrl.Should().Be("https://pay.oxapay.test/TRK1");
        result.Value.PaymentId.Should().Be(5);
    }

    [Fact]
    public async Task GetInvoiceStatus_EmptyTrackId_ReturnsFail()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, "{}");

        var result = await service.GetInvoiceStatus(string.Empty);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvoiceStatus_MissingMerchantApiKey_ReturnsFail()
    {
        var config = BuildConfiguration(new Dictionary<string, string?> { ["OxaPay:MerchantApiKey"] = null });
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", config);

        var result = await service.GetInvoiceStatus("TRK1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvoiceStatus_HttpErrorResponse_ReturnsFail()
    {
        var errorJson = """{"data":null,"message":"not found","status":404}""";
        var (service, _) = CreateService(HttpStatusCode.NotFound, errorJson);

        var result = await service.GetInvoiceStatus("TRK1");

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("not found");
    }

    [Theory]
    [InlineData("paid", PaymentStatus.Completed)]
    [InlineData("confirmed", PaymentStatus.Completed)]
    [InlineData("expired", PaymentStatus.Failed)]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("refunded", PaymentStatus.Refunded)]
    [InlineData("waiting", PaymentStatus.Pending)]
    public async Task GetInvoiceStatus_MapsOxaPayStatusToLocalStatus(string oxaPayStatus, PaymentStatus expected)
    {
        var json = $$"""
        {"data":{"track_id":"TRK1","type":"invoice","amount":9.99,"currency":"USD","status":"{{oxaPayStatus}}","expired_at":1999999999,"date":1999999000,"order_id":"ORD1","description":"desc"},"message":"ok","status":200}
        """;
        var (service, _) = CreateService(HttpStatusCode.OK, json);

        var result = await service.GetInvoiceStatus("TRK1");

        result.IsSuccess.Should().BeTrue();
        result.Value.LocalStatus.Should().Be(expected);
    }
}

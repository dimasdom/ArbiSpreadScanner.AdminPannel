using System.Net;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class OxaPayServiceTests
{
    private static OxaPaySettings BuildSettings(Action<OxaPaySettings>? configure = null)
    {
        var settings = new OxaPaySettings
        {
            MerchantApiKey = "merchant-key",
            BaseUrl = "https://api.oxapay.test",
            DefaultCurrency = "USD",
            DefaultLifetime = 30,
            Sandbox = true
        };
        configure?.Invoke(settings);
        return settings;
    }

    private static (OxaPayService Service, Mock<IHttpClientFactory> Factory) CreateService(
        HttpStatusCode statusCode, string content, OxaPaySettings? settings = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, content);
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new OxaPayService(factory.Object, Options.Create(settings ?? BuildSettings()), NullLogger<OxaPayService>.Instance);
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
        var settings = BuildSettings(s => s.MerchantApiKey = "");
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);

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
        var settings = BuildSettings(s => s.MerchantApiKey = "");
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);

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

    private static string ComputeExpectedHmac(string body, string key) =>
        Convert.ToHexStringLower(System.Security.Cryptography.HMACSHA512.HashData(
            System.Text.Encoding.UTF8.GetBytes(key), System.Text.Encoding.UTF8.GetBytes(body)));

    [Fact]
    public void VerifyWebhookSignature_MatchingHmac_ReturnsTrue()
    {
        var settings = BuildSettings();
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);
        const string body = """{"track_id":"TRK1","status":"Paid","type":"invoice","date":1999999000}""";
        var hmac = ComputeExpectedHmac(body, settings.MerchantApiKey);

        service.VerifyWebhookSignature(body, hmac).Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhookSignature_HmacComputedWithWrongKey_ReturnsFalse()
    {
        var settings = BuildSettings();
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);
        const string body = """{"track_id":"TRK1","status":"Paid","type":"invoice","date":1999999000}""";
        var hmac = ComputeExpectedHmac(body, "wrong-key");

        service.VerifyWebhookSignature(body, hmac).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_BodyTamperedAfterSigning_ReturnsFalse()
    {
        var settings = BuildSettings();
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);
        const string originalBody = """{"track_id":"TRK1","status":"Paid","type":"invoice","date":1999999000}""";
        const string tamperedBody = """{"track_id":"TRK1","status":"Paid","type":"invoice","date":9999999999}""";
        var hmac = ComputeExpectedHmac(originalBody, settings.MerchantApiKey);

        service.VerifyWebhookSignature(tamperedBody, hmac).Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_MissingHeader_ReturnsFalse()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, "{}");

        service.VerifyWebhookSignature("{}", null).Should().BeFalse();
        service.VerifyWebhookSignature("{}", "").Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_MissingMerchantApiKey_ReturnsFalse()
    {
        var settings = BuildSettings(s => s.MerchantApiKey = "");
        var (service, _) = CreateService(HttpStatusCode.OK, "{}", settings);

        service.VerifyWebhookSignature("{}", "anything").Should().BeFalse();
    }
}

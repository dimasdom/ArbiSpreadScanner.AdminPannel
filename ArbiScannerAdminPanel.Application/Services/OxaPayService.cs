using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArbiScannerAdminPanel.Application.Services;

public class OxaPayService : IOxaPayService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OxaPayService> _logger;

    public OxaPayService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OxaPayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<OxaPayInvoiceResultDTO>> GenerateInvoice(
        UserSubscriptionPayment userPayment,
        string? userEmail,
        OxaPayInvoiceCreateOptionsDTO? options = null)
    {
        try
        {
            if (userPayment.Payment == null)
            {
                _logger.LogWarning("GenerateInvoice failed: payment model not found for UserSubscriptionPaymentId {Id}", userPayment.Id);
                return Result.Fail<OxaPayInvoiceResultDTO>("Payment model not found");
            }

            var merchantApiKey = _configuration["OxaPay:MerchantApiKey"];
            if (string.IsNullOrWhiteSpace(merchantApiKey))
            {
                _logger.LogError("GenerateInvoice failed: OxaPay merchant API key is not configured");
                return Result.Fail<OxaPayInvoiceResultDTO>("OxaPay merchant API key is not configured");
            }

            var payload = BuildInvoicePayload(userPayment, options, userEmail);

            var client = CreateOxaPayClient(merchantApiKey);
            var response = await client.PostAsJsonAsync("payment/invoice", payload);

            var responseBody = await response.Content.ReadFromJsonAsync<OxaPayApiResponse<OxaPayInvoiceData>>();
            if (!response.IsSuccessStatusCode || responseBody?.Data == null)
            {
                var errorMessage = responseBody?.Message ?? "Failed to generate invoice in OxaPay";
                _logger.LogError("GenerateInvoice failed for payment {PaymentId}: {Error} (HTTP {StatusCode})",
                    userPayment.Payment.Id, errorMessage, (int)response.StatusCode);
                return Result.Fail<OxaPayInvoiceResultDTO>(errorMessage);
            }

            return Result.Ok(new OxaPayInvoiceResultDTO
            {
                UserSubscriptionPaymentId = userPayment.Id,
                PaymentId = userPayment.Payment.Id,
                TrackId = responseBody.Data.TrackId,
                PaymentUrl = responseBody.Data.PaymentUrl,
                ExpiredAt = responseBody.Data.ExpiredAt,
                Date = responseBody.Data.Date
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateInvoice threw for UserSubscriptionPaymentId {Id}", userPayment.Id);
            return Result.Fail<OxaPayInvoiceResultDTO>($"Failed to generate invoice: {ex.Message}");
        }
    }

    public async Task<Result<OxaPayPaymentStatusDTO>> GetInvoiceStatus(string trackId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                _logger.LogWarning("GetInvoiceStatus failed: trackId is empty");
                return Result.Fail<OxaPayPaymentStatusDTO>("TrackId is required");
            }

            var merchantApiKey = _configuration["OxaPay:MerchantApiKey"];
            if (string.IsNullOrWhiteSpace(merchantApiKey))
            {
                _logger.LogError("GetInvoiceStatus failed: OxaPay merchant API key is not configured");
                return Result.Fail<OxaPayPaymentStatusDTO>("OxaPay merchant API key is not configured");
            }

            var client = CreateOxaPayClient(merchantApiKey);
            var response = await client.GetAsync($"payment/{trackId}");
            var responseBody = await response.Content.ReadFromJsonAsync<OxaPayApiResponse<OxaPayPaymentInfoData>>();

            if (!response.IsSuccessStatusCode || responseBody?.Data == null)
            {
                var errorMessage = responseBody?.Message ?? "Failed to get invoice status from OxaPay";
                _logger.LogError("GetInvoiceStatus failed for trackId {TrackId}: {Error} (HTTP {StatusCode})",
                    trackId, errorMessage, (int)response.StatusCode);
                return Result.Fail<OxaPayPaymentStatusDTO>(errorMessage);
            }

            return Result.Ok(new OxaPayPaymentStatusDTO
            {
                TrackId = responseBody.Data.TrackId,
                Type = responseBody.Data.Type,
                Amount = responseBody.Data.Amount,
                Currency = responseBody.Data.Currency,
                Status = responseBody.Data.Status,
                ExpiredAt = responseBody.Data.ExpiredAt,
                Date = responseBody.Data.Date,
                OrderId = responseBody.Data.OrderId,
                Description = responseBody.Data.Description,
                LocalStatus = MapOxaPayStatus(responseBody.Data.Status)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInvoiceStatus threw for trackId {TrackId}", trackId);
            return Result.Fail<OxaPayPaymentStatusDTO>($"Failed to get invoice status: {ex.Message}");
        }
    }

    private HttpClient CreateOxaPayClient(string merchantApiKey)
    {
        var baseUrl = _configuration["OxaPay:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("OxaPay:BaseUrl is not configured");
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");
        client.DefaultRequestHeaders.Remove("merchant_api_key");
        client.DefaultRequestHeaders.Add("merchant_api_key", merchantApiKey.Trim());
        return client;
    }

    private Dictionary<string, object?> BuildInvoicePayload(UserSubscriptionPayment userPayment, OxaPayInvoiceCreateOptionsDTO? options, string? userEmail)
    {
        var defaultLifetime = _configuration.GetValue("OxaPay:DefaultLifetime", 30);
        var lifetime = Math.Clamp(options?.Lifetime ?? defaultLifetime, 15, 2880);

        var payload = new Dictionary<string, object?>
        {
            ["amount"] = userPayment.Payment!.Amount,
            ["currency"] = options?.Currency ?? _configuration["OxaPay:DefaultCurrency"] ?? "USD",
            ["lifetime"] = lifetime,
            ["sandbox"] = options?.Sandbox ?? _configuration.GetValue("OxaPay:Sandbox", true),
            ["order_id"] = options?.OrderId ?? $"PAY-{userPayment.PaymentId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        };

        if (options?.FeePaidByPayer is not null) payload["fee_paid_by_payer"] = options.FeePaidByPayer;
        if (options?.UnderPaidCoverage is not null) payload["under_paid_coverage"] = options.UnderPaidCoverage;
        if (!string.IsNullOrWhiteSpace(options?.ToCurrency)) payload["to_currency"] = options.ToCurrency;
        if (options?.AutoWithdrawal is not null) payload["auto_withdrawal"] = options.AutoWithdrawal;
        if (options?.MixedPayment is not null) payload["mixed_payment"] = options.MixedPayment;
        if (!string.IsNullOrWhiteSpace(options?.CallbackUrl)) payload["callback_url"] = options.CallbackUrl;
        if (!string.IsNullOrWhiteSpace(options?.ReturnUrl)) payload["return_url"] = options.ReturnUrl;
        if (!string.IsNullOrWhiteSpace(userEmail)) payload["email"] = userEmail;
        if (!string.IsNullOrWhiteSpace(options?.ThanksMessage)) payload["thanks_message"] = options.ThanksMessage;
        if (!string.IsNullOrWhiteSpace(options?.Description)) payload["description"] = options.Description;

        return payload;
    }

    private static PaymentStatus MapOxaPayStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "paid" => PaymentStatus.Completed,
            "confirmed" => PaymentStatus.Completed,
            "expired" => PaymentStatus.Failed,
            "failed" => PaymentStatus.Failed,
            "refunded" => PaymentStatus.Refunded,
            _ => PaymentStatus.Pending
        };
    }

    private sealed class OxaPayApiResponse<TData>
    {
        [JsonPropertyName("data")]
        public TData? Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public JsonElement Error { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    private sealed class OxaPayInvoiceData
    {
        [JsonPropertyName("track_id")]
        public string TrackId { get; set; } = string.Empty;

        [JsonPropertyName("payment_url")]
        public string PaymentUrl { get; set; } = string.Empty;

        [JsonPropertyName("expired_at")]
        public long ExpiredAt { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }
    }

    private sealed class OxaPayPaymentInfoData
    {
        [JsonPropertyName("track_id")]
        public string TrackId { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("expired_at")]
        public long ExpiredAt { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}

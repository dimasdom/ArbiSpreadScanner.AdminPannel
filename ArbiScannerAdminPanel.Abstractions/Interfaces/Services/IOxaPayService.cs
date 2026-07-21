using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services;

public interface IOxaPayService
{
    Task<Result<OxaPayInvoiceResultDTO>> GenerateInvoice(
        UserSubscriptionPayment userPayment,
        string? userEmail,
        OxaPayInvoiceCreateOptionsDTO? options = null);

    Task<Result<OxaPayPaymentStatusDTO>> GetInvoiceStatus(string trackId);

    /// <summary>
    /// Verifies the HMAC-SHA512 signature OxaPay sends with payment webhooks (the "HMAC" header),
    /// computed over the raw request body using the merchant API key as the shared secret.
    /// </summary>
    bool VerifyWebhookSignature(string rawRequestBody, string? hmacHeader);
}

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
}

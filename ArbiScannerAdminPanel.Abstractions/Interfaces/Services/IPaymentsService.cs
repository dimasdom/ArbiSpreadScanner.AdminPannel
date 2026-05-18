using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services
{
    public interface IPaymentsService
    {
        Task<Result<List<PaymentModel>>> GetAllPayments(int page = 1);
        Task<Result<PaymentModel>> GetPaymentById(int id);
        Task<Result<PaymentResultDTO>> GetPaymentDTOById(int id);
        Task<Result> RemovePayments(List<int> id);
        Task<Result<List<UserSubscriptionPayment>>> GetPaymentsForUser(string userId);
        Task<Result<UserSubscriptionPayment>> CreatePaymentForUser(UserSubscriptionPayment payment);
        Task<Result<UserSubscriptionPayment>> GetActivePaymentForUser(string userId);
        Task<Result<UserSubscriptionPayment>> AcceptPayment(string TransactionId);
        Task<Result<UserSubscriptionPayment>> GetUserPaymentByIdAsync(int paymentId);
        Task<Result> CancelPayment(int userSubscriptionPaymentId);
        Task<Result<OxaPayInvoiceResultDTO>> GenerateInvoice(int userSubscriptionPaymentId, OxaPayInvoiceCreateOptionsDTO? options = null);
        Task<Result<OxaPayPaymentStatusDTO>> GetInvoiceStatus(string trackId);
    }
}

using ArbiScannerAdminPanel.Domain.Models;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;

public interface IPaymentsRepository
{
    Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByTransactionId(string transactionId, bool forUpdate = false);

    Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByPaymentId(int paymentId, bool forUpdate = false);

    Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentWithDetails(int userSubscriptionPaymentId, bool forUpdate = false);

    Task<UserSubscriptionPayment?> GetActiveUserPayment(string userId, DateTime utcNow, bool forUpdate = false);

    Task<List<UserSubscriptionPayment>> GetPaymentsForUser(string userId);

    Task<List<PaymentModel>> GetAllPayments(int page, int pageSize = 20);

    Task<List<PaymentModel>> GetPaymentsByIds(List<int> ids);

    Task<PaymentModel?> GetPaymentById(int paymentId);

    Task AddUserSubscriptionPayment(UserSubscriptionPayment payment);

    void RemovePayment(PaymentModel payment);

    void RemovePayments(List<PaymentModel> payments);

    Task SaveChangesAsync();
}

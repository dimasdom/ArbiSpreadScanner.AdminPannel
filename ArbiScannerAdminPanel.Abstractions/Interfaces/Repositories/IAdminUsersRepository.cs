using ArbiScannerAdminPanel.Domain.Models;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;

public interface IAdminUsersRepository
{
    Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByUserId(string userId);

    Task<List<PaymentModel>> GetPaymentsByUserId(string userId);

    Task<UserSubscriptionModel?> GetUserSubscriptionByUserId(string userId);
}

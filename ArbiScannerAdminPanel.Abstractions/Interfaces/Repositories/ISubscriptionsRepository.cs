using ArbiScannerAdminPanel.Domain.Models;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;

public interface ISubscriptionsRepository
{
    Task<SubscriptionModel?> GetSubscriptionById(int subscriptionId);

    Task<List<SubscriptionModel>> GetAllSubscriptions(int page, int pageSize = 20);

    Task AddSubscription(SubscriptionModel subscription);

    Task<List<SubscriptionModel>> GetSubscriptionsByIds(List<int> ids);

    void RemoveSubscriptions(List<SubscriptionModel> subscriptions);

    Task<UserSubscriptionModel?> GetUserSubscriptionById(int userSubscriptionId);

    Task<UserSubscriptionModel?> GetLatestUserSubscriptionByUserId(string userId);

    Task<List<UserSubscriptionModel>> GetAllUserSubscriptionsWithSubscription(int page, int pageSize = 20);

    Task<List<UserSubscriptionModel>> GetUserSubscriptionsByIds(List<int> ids);

    Task AddUserSubscription(UserSubscriptionModel userSubscription);

    void RemoveUserSubscriptions(List<UserSubscriptionModel> userSubscriptions);

    Task SaveChangesAsync();
}

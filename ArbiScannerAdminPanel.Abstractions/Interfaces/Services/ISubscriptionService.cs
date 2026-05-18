using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task<Result<List<SubscriptionModel>>> GetAllSubscriptions(int page = 1);
        Task<Result<SubscriptionModel>> GetSubscriptionById(int id);
        Task<Result> CreateSubscription(SubscriptionModel subscriptionModel);
        Task<Result> UpdateSubscription(SubscriptionModel subscriptionModel);
        Task<Result> DeleteSubscriptionsById(List<int> id);
        Task<Result<UserSubscriptionModel>> GetUserSubscriptionByUserId(string userId);
        Task<Result<UserSubscriptionModel>> CreateUserSubscription(UserSubscriptionCreateDTO userSubscriptionCreateDTO);
        Task<Result<UserSubscriptionModel>> AssignSubscriptionToUser(UserSubscriptionPayment userSubscriptionPayment);
        Task<Result<List<UserSubscriptionRowDTO>>> GetAllUserSubscriptions(int page = 1);
        Task<Result> UpdateUserSubscription(UserSubscriptionModel userSubscriptionModel);
        Task<Result> DeleteUserSubscriptionsById(List<int> ids);
        Task<Result<UserSubscriptionModel>> GetUserSubscriptionById(int id);
    }
}

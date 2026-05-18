using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbiScannerAdminPanel.Application.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IWebAppUserRepository _webAppUserRepository;
        private readonly IDatabase _redis;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(ISubscriptionsRepository subscriptionsRepository, IWebAppUserRepository webAppUserRepository, IConnectionMultiplexer redis, ILogger<SubscriptionService> logger)
        {
            _subscriptionsRepository = subscriptionsRepository;
            _webAppUserRepository = webAppUserRepository;
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<Result<UserSubscriptionModel>> AssignSubscriptionToUser(UserSubscriptionPayment userSubscriptionPayment)
        {
            var subscription = await _subscriptionsRepository.GetSubscriptionById(userSubscriptionPayment.SubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("AssignSubscriptionToUser failed: subscription {SubscriptionId} not found", userSubscriptionPayment.SubscriptionId);
                return Result.Fail<UserSubscriptionModel>("Subscription not found");
            }
            var userSubscriptionModel = new UserSubscriptionModel
            {
                UserId = userSubscriptionPayment.UserId,
                SubscriptionId = userSubscriptionPayment.SubscriptionId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(subscription.DurationInDays)
            };
            await _subscriptionsRepository.AddUserSubscription(userSubscriptionModel);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok(userSubscriptionModel);
        }

        public async Task<Result> CreateSubscription(SubscriptionModel subscriptionModel)
        {
            var subscription = new SubscriptionModel();
            subscription.Price = subscriptionModel.Price;
            subscription.Type = subscriptionModel.Type;
            subscription.DurationInDays = subscriptionModel.DurationInDays;
            await _subscriptionsRepository.AddSubscription(subscription);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<UserSubscriptionModel>> CreateUserSubscription(UserSubscriptionCreateDTO userSubscriptionCreateDTO)
        {
            var userResult = await _webAppUserRepository.GetByEmail(userSubscriptionCreateDTO.UserEmail);
            if (userResult.IsFailed)
            {
                return Result.Fail<UserSubscriptionModel>(userResult.Errors);
            }

            var user = userResult.Value;
            if (user == null)
            {
                _logger.LogWarning("CreateUserSubscription failed: user with email {Email} not found", userSubscriptionCreateDTO.UserEmail);
                return Result.Fail<UserSubscriptionModel>("User not found");
            }
            var subscription = await _subscriptionsRepository.GetSubscriptionById(userSubscriptionCreateDTO.SubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("CreateUserSubscription failed: subscription {SubscriptionId} not found", userSubscriptionCreateDTO.SubscriptionId);
                return Result.Fail<UserSubscriptionModel>("Subscription not found");
            }
            var userSubscriptionModel = new UserSubscriptionModel
            {
                UserId = user.Id,
                SubscriptionId = subscription.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(subscription.DurationInDays)
            };
            await _subscriptionsRepository.AddUserSubscription(userSubscriptionModel);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok(userSubscriptionModel);
        }

        public async Task<Result> DeleteSubscriptionsById(List<int> id)
        {
            var subscriptions = await _subscriptionsRepository.GetSubscriptionsByIds(id);
            _subscriptionsRepository.RemoveSubscriptions(subscriptions);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> DeleteUserSubscriptionsById(List<int> ids)
        {
            var userSubscriptions = await _subscriptionsRepository.GetUserSubscriptionsByIds(ids);
            _subscriptionsRepository.RemoveUserSubscriptions(userSubscriptions);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<List<SubscriptionModel>>> GetAllSubscriptions(int page = 1)
        {
            var subscriptions = await _subscriptionsRepository.GetAllSubscriptions(page);
            return Result.Ok(subscriptions);
        }

        public async Task<Result<List<UserSubscriptionRowDTO>>> GetAllUserSubscriptions(int page = 1)
        {
            var userSubscriptions = await _subscriptionsRepository.GetAllUserSubscriptionsWithSubscription(page);
            var userSubscriptionRows = new List<UserSubscriptionRowDTO>();
            foreach(var userSubscription in userSubscriptions)
            {
                var userResult = await _webAppUserRepository.GetById(userSubscription.UserId);
                var user = userResult.IsSuccess ? userResult.Value : null;
                var userSubscriptionRow = new UserSubscriptionRowDTO
                {
                    Id = userSubscription.Id.ToString(),
                    UserMail = user?.Email ?? "Unknown",
                    SubcriptionType = userSubscription.Subscription?.Type ?? "Unknown",
                    SubscriptionStartDate = userSubscription.StartDate,
                    SubscriptionEndDate = userSubscription.EndDate
                };
                userSubscriptionRows.Add(userSubscriptionRow);
            }
            return Result.Ok(userSubscriptionRows);
        }

        public async Task<Result<SubscriptionModel>> GetSubscriptionById(int id)
        {
            var subscription = await _subscriptionsRepository.GetSubscriptionById(id);
            if (subscription == null)
            {
                _logger.LogWarning("GetSubscriptionById failed: subscription {SubscriptionId} not found", id);
                return Result.Fail<SubscriptionModel>("Subscription not found");
            }
            return Result.Ok(subscription);
        }

        public async Task<Result<UserSubscriptionModel>> GetUserSubscriptionById(int id)
        {
            var userSubscription = await _subscriptionsRepository.GetUserSubscriptionById(id);
            if (userSubscription == null)
            {
                _logger.LogWarning("GetUserSubscriptionById failed: user subscription {Id} not found", id);
                return Result.Fail<UserSubscriptionModel>("User subscription not found");
            }
            return Result.Ok(userSubscription);
        }

        public async Task<Result<UserSubscriptionModel>> GetUserSubscriptionByUserId(string userId)
        {
            var user = await _redis.StringGetAsync($"userSubscription:{userId}");
            if (!user.IsNullOrEmpty)
            {
                var cachedSubscription = Newtonsoft.Json.JsonConvert.DeserializeObject<UserSubscriptionModel>((string)user!);
                if (cachedSubscription != null && cachedSubscription.EndDate > DateTime.UtcNow)
                {
                    return Result.Ok(cachedSubscription);
                }
            }
            var userSubscription = await _subscriptionsRepository.GetLatestUserSubscriptionByUserId(userId);
            if(userSubscription == null)
            {
                _logger.LogWarning("GetUserSubscriptionByUserId failed: no subscription found for user {UserId}", userId);
                return Result.Fail<UserSubscriptionModel>("User subscription not found");
            }
            await _redis.StringSetAsync($"userSubscription:{userId}", Newtonsoft.Json.JsonConvert.SerializeObject(userSubscription), TimeSpan.FromDays(7));
            return Result.Ok(userSubscription);
        }

        public async Task<Result> UpdateSubscription(SubscriptionModel subscriptionModel)
        {
            var subscription = await _subscriptionsRepository.GetSubscriptionById(subscriptionModel.Id);
            if (subscription == null)
            {
                _logger.LogWarning("UpdateSubscription failed: subscription {SubscriptionId} not found", subscriptionModel.Id);
                return Result.Fail("Subscription not found");
            }
            subscription.Price = subscriptionModel.Price;
            subscription.Type = subscriptionModel.Type;
            subscription.DurationInDays = subscriptionModel.DurationInDays;
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> UpdateUserSubscription(UserSubscriptionModel userSubscriptionModel)
        {
            await _redis.KeyDeleteAsync($"userSubscription:{userSubscriptionModel.UserId}");
            var userSubscription = await _subscriptionsRepository.GetUserSubscriptionById(userSubscriptionModel.Id);
            if (userSubscription == null)
            {
                _logger.LogWarning("UpdateUserSubscription failed: user subscription {Id} not found", userSubscriptionModel.Id);
                return Result.Fail("User subscription not found");
            }
            userSubscription.EndDate = new DateTime(userSubscriptionModel.EndDate.Year, userSubscriptionModel.EndDate.Month, userSubscriptionModel.EndDate.Day, 0, 0, 0, DateTimeKind.Utc);
            await _subscriptionsRepository.SaveChangesAsync();
            return Result.Ok();
        }
    }
}

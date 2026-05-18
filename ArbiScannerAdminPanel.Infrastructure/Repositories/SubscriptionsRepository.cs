using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories;

public class SubscriptionsRepository : ISubscriptionsRepository
{
    private readonly AdminPanelAppDbContext _dbContext;

    public SubscriptionsRepository(AdminPanelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubscriptionModel?> GetSubscriptionById(int subscriptionId)
    {
        return await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<List<SubscriptionModel>> GetAllSubscriptions(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.Subscriptions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddSubscription(SubscriptionModel subscription)
    {
        await _dbContext.Subscriptions.AddAsync(subscription);
    }

    public async Task<List<SubscriptionModel>> GetSubscriptionsByIds(List<int> ids)
    {
        return await _dbContext.Subscriptions.Where(s => ids.Contains(s.Id)).ToListAsync();
    }

    public void RemoveSubscriptions(List<SubscriptionModel> subscriptions)
    {
        _dbContext.Subscriptions.RemoveRange(subscriptions);
    }

    public async Task<UserSubscriptionModel?> GetUserSubscriptionById(int userSubscriptionId)
    {
        return await _dbContext.UserSubscriptions
            .Include(usp => usp.Subscription)
            .FirstOrDefaultAsync(usp => usp.Id == userSubscriptionId);
    }

    public async Task<UserSubscriptionModel?> GetLatestUserSubscriptionByUserId(string userId)
    {
        return await _dbContext.UserSubscriptions
            .Include(usp => usp.Subscription)
            .OrderByDescending(usp => usp.EndDate)
            .FirstOrDefaultAsync(usp => usp.UserId == userId);
    }

    public async Task<List<UserSubscriptionModel>> GetAllUserSubscriptionsWithSubscription(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.UserSubscriptions
            .Include(usr => usr.Subscription)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<UserSubscriptionModel>> GetUserSubscriptionsByIds(List<int> ids)
    {
        return await _dbContext.UserSubscriptions.Where(usp => ids.Contains(usp.Id)).ToListAsync();
    }

    public async Task AddUserSubscription(UserSubscriptionModel userSubscription)
    {
        await _dbContext.UserSubscriptions.AddAsync(userSubscription);
    }

    public void RemoveUserSubscriptions(List<UserSubscriptionModel> userSubscriptions)
    {
        _dbContext.UserSubscriptions.RemoveRange(userSubscriptions);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}

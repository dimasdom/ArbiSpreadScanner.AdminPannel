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

    private IQueryable<SubscriptionModel> SubscriptionsQuery(bool forUpdate) =>
        forUpdate
            ? _dbContext.Subscriptions.AsTracking()
            : _dbContext.Subscriptions.AsNoTracking();

    private IQueryable<UserSubscriptionModel> UserSubscriptionsQuery(bool forUpdate) =>
        forUpdate
            ? _dbContext.UserSubscriptions.AsTracking()
            : _dbContext.UserSubscriptions.AsNoTracking();

    public async Task<SubscriptionModel?> GetSubscriptionById(int subscriptionId, bool forUpdate = false)
    {
        return await SubscriptionsQuery(forUpdate)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);
    }

    public async Task<List<SubscriptionModel>> GetAllSubscriptions(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.Subscriptions
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task AddSubscription(SubscriptionModel subscription)
    {
        _dbContext.Subscriptions.Add(subscription);
        return Task.CompletedTask;
    }

    public async Task<List<SubscriptionModel>> GetSubscriptionsByIds(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return [];

        return await _dbContext.Subscriptions
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();
    }

    public void RemoveSubscriptions(List<SubscriptionModel> subscriptions)
    {
        _dbContext.Subscriptions.RemoveRange(subscriptions);
    }

    public async Task<UserSubscriptionModel?> GetUserSubscriptionById(int userSubscriptionId, bool forUpdate = false)
    {
        return await UserSubscriptionsQuery(forUpdate)
            .Include(usp => usp.Subscription)
            .FirstOrDefaultAsync(usp => usp.Id == userSubscriptionId);
    }

    public async Task<UserSubscriptionModel?> GetLatestUserSubscriptionByUserId(string userId)
    {
        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Include(usp => usp.Subscription)
            .OrderByDescending(usp => usp.EndDate)
            .FirstOrDefaultAsync(usp => usp.UserId == userId);
    }

    public async Task<List<UserSubscriptionModel>> GetAllUserSubscriptionsWithSubscription(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Include(usr => usr.Subscription)
            .OrderByDescending(usr => usr.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<UserSubscriptionModel>> GetUserSubscriptionsByIds(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return [];

        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(usp => ids.Contains(usp.Id))
            .ToListAsync();
    }

    public Task AddUserSubscription(UserSubscriptionModel userSubscription)
    {
        _dbContext.UserSubscriptions.Add(userSubscription);
        return Task.CompletedTask;
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

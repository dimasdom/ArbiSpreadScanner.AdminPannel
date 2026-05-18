using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories;

public class AdminUsersRepository : IAdminUsersRepository
{
    private readonly AdminPanelAppDbContext _dbContext;

    public AdminUsersRepository(AdminPanelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByUserId(string userId)
    {
        return await _dbContext.UserSubscriptionPayments
            .Include(usp => usp.Subscription)
            .Include(usp => usp.Payment)
            .FirstOrDefaultAsync(usp => usp.UserId == userId);
    }

    public async Task<List<PaymentModel>> GetPaymentsByUserId(string userId)
    {
        return await _dbContext.Payments.Where(pm => pm.UserId == userId).ToListAsync();
    }

    public async Task<UserSubscriptionModel?> GetUserSubscriptionByUserId(string userId)
    {
        return await _dbContext.UserSubscriptions
            .OrderByDescending(usp => usp.EndDate)
            .FirstOrDefaultAsync(usp => usp.UserId == userId);
    }
}

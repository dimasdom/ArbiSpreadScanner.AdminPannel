using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly AdminPanelAppDbContext _dbContext;

    public PaymentsRepository(AdminPanelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<UserSubscriptionPayment> BaseQuery(bool forUpdate) =>
        forUpdate
            ? _dbContext.UserSubscriptionPayments.AsTracking()
            : _dbContext.UserSubscriptionPayments.AsNoTracking();

    public async Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByTransactionId(string transactionId, bool forUpdate = false)
    {
        return await BaseQuery(forUpdate)
            .Include(usp => usp.Payment)
            .FirstOrDefaultAsync(usp => usp.Payment != null && usp.Payment.TransactionId == transactionId);
    }

    public async Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentByPaymentId(int paymentId, bool forUpdate = false)
    {
        return await BaseQuery(forUpdate)
            .Include(usp => usp.Payment)
            .FirstOrDefaultAsync(usp => usp.Payment != null && usp.Payment.Id == paymentId);
    }

    public async Task<UserSubscriptionPayment?> GetUserSubscriptionPaymentWithDetails(int userSubscriptionPaymentId, bool forUpdate = false)
    {
        return await BaseQuery(forUpdate)
            .Include(usp => usp.Payment)
            .Include(usp => usp.Subscription)
            .FirstOrDefaultAsync(usp => usp.Id == userSubscriptionPaymentId);
    }

    public async Task<UserSubscriptionPayment?> GetActiveUserPayment(string userId, DateTime utcNow, bool forUpdate = false)
    {
        return await BaseQuery(forUpdate)
            .Include(usp => usp.Payment)
            .Include(usp => usp.Subscription)
            .Where(usp => usp.UserId == userId &&
                          usp.Payment != null &&
                          usp.Payment.Status == PaymentStatus.Pending &&
                          usp.ExpirationDate > utcNow)
            .OrderByDescending(usp => usp.Payment!.PaymentDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserSubscriptionPayment>> GetPaymentsForUser(string userId)
    {
        return await _dbContext.UserSubscriptionPayments
            .AsNoTracking()
            .Include(p => p.Payment)
            .Include(p => p.Subscription)
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<PaymentModel>> GetAllPayments(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<PaymentModel>> GetPaymentsByIds(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
            return [];

        return await _dbContext.Payments
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<PaymentModel?> GetPaymentById(int paymentId)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public Task AddUserSubscriptionPayment(UserSubscriptionPayment payment)
    {
        _dbContext.UserSubscriptionPayments.Add(payment);
        return Task.CompletedTask;
    }

    public void RemovePayment(PaymentModel payment)
    {
        _dbContext.Payments.Remove(payment);
    }

    public void RemovePayments(List<PaymentModel> payments)
    {
        _dbContext.Payments.RemoveRange(payments);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}

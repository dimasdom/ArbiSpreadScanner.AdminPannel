using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories;

public class WebAppUserRepository : IWebAppUserRepository
{
    private readonly AppDbContext _dbContext;

    public WebAppUserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebAppUserDTO?> GetById(string userId)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return null;

        return new WebAppUserDTO
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public async Task<WebAppUserDTO?> GetByEmail(string email)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user == null)
            return null;

        return new WebAppUserDTO
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
    }

    public async Task<List<WebAppUserDTO>> GetUsers(int page, int pageSize = 20)
    {
        page = page < 1 ? 1 : page;
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new WebAppUserDTO
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task<List<WebAppUserDTO>> SearchByEmail(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Email)
            .Select(u => new WebAppUserDTO
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email
            })
            .ToListAsync();
    }

    public async Task UpdateUser(string userId, string? email, string? userName)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        if (email != null)
        {
            user.Email = email;
            user.NormalizedEmail = email.ToUpperInvariant();
        }

        if (userName != null)
        {
            user.UserName = userName;
            user.NormalizedUserName = userName.ToUpperInvariant();
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUser(string userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUsers(List<string> ids)
    {
        var users = await _dbContext.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        if (users.Count == 0)
            throw new KeyNotFoundException("No users found with the provided ids");

        _dbContext.Users.RemoveRange(users);
        await _dbContext.SaveChangesAsync();
    }
}

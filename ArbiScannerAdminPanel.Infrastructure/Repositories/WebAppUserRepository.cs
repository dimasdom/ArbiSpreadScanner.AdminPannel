using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.DbContext;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories;

public class WebAppUserRepository : IWebAppUserRepository
{
    private readonly AppDbContext _dbContext;

    public WebAppUserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WebAppUserDTO?>> GetById(string userId)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result.Ok<WebAppUserDTO?>(null);

            return Result.Ok<WebAppUserDTO?>(new WebAppUserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            return Result.Fail<WebAppUserDTO?>($"Failed to get user by id: {ex.Message}");
        }
    }

    public async Task<Result<WebAppUserDTO?>> GetByEmail(string email)
    {
        try
        {
            var normalizedEmail = email.ToUpperInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (user == null)
                return Result.Ok<WebAppUserDTO?>(null);

            return Result.Ok<WebAppUserDTO?>(new WebAppUserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            return Result.Fail<WebAppUserDTO?>($"Failed to get user by email: {ex.Message}");
        }
    }

    public async Task<Result<List<WebAppUserDTO>>> GetUsers(int page, int pageSize = 20)
    {
        try
        {
            page = page < 1 ? 1 : page;
            var users = await _dbContext.Users
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

            return Result.Ok(users);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<WebAppUserDTO>>($"Failed to get users: {ex.Message}");
        }
    }

    public async Task<Result<List<WebAppUserDTO>>> SearchByEmail(string email)
    {
        try
        {
            var emailLower = email.ToLowerInvariant();
            var users = await _dbContext.Users
                .Where(u => u.Email != null && u.Email.ToLower().Contains(emailLower))
                .OrderBy(u => u.Email)
                .Select(u => new WebAppUserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email
                })
                .ToListAsync();

            return Result.Ok(users);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<WebAppUserDTO>>($"Failed to search users by email: {ex.Message}");
        }
    }

    public async Task<Result> UpdateUser(string userId, string? email, string? userName)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result.Fail("User not found");

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
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update user: {ex.Message}");
        }
    }

    public async Task<Result> DeleteUser(string userId)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result.Fail("User not found");

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to delete user: {ex.Message}");
        }
    }

    public async Task<Result> DeleteUsers(List<string> ids)
    {
        try
        {
            var users = await _dbContext.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            if (users.Count == 0)
                return Result.Fail("No users found with the provided ids");

            _dbContext.Users.RemoveRange(users);
            await _dbContext.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to delete users: {ex.Message}");
        }
    }
}

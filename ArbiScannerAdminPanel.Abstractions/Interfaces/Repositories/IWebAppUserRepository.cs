using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;

public interface IWebAppUserRepository
{
    Task<Result<WebAppUserDTO?>> GetById(string userId);

    Task<Result<WebAppUserDTO?>> GetByEmail(string email);

    Task<Result<List<WebAppUserDTO>>> GetUsers(int page, int pageSize = 20);

    Task<Result<List<WebAppUserDTO>>> SearchByEmail(string email);

    Task<Result> UpdateUser(string userId, string? email, string? userName);

    Task<Result> DeleteUser(string userId);

    Task<Result> DeleteUsers(List<string> ids);
}

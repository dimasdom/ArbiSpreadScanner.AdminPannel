using ArbiScannerAdminPanel.Domain.Models.DTOs;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;

public interface IWebAppUserRepository
{
    Task<WebAppUserDTO?> GetById(string userId);

    Task<WebAppUserDTO?> GetByEmail(string email);

    Task<List<WebAppUserDTO>> GetUsers(int page, int pageSize = 20);

    Task<List<WebAppUserDTO>> SearchByEmail(string email);

    Task UpdateUser(string userId, string? email, string? userName);

    Task DeleteUser(string userId);

    Task DeleteUsers(List<string> ids);
}

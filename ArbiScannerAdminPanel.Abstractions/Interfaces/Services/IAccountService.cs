using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services
{
    public interface IAccountService
    {
        Task<Result<AdminAccountDTO>> AuthenticateAdmin(AdminAccountAuthenticateDTO accountAuthenticateDTO);
        Task<Result<AdminRefreshTokenResponse>> RefreshAccessToken(string refreshToken, string? ipAddress = null, string? userAgent = null);
        Task<Result> LogoutByToken(string userId, string refreshToken, string? ipAddress = null);
        Task<Result<AdminAccountDTO>> GetCurrentAdmin(string userId);
    }
}

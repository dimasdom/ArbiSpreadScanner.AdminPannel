using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services
{
    public interface IKeycloakUserService
    {
        Task<Result> DeleteUserAsync(string userId);
    }
}

using ArbiScannerAdminPanel.Domain.Models;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories
{
    public interface IAdminRefreshTokenRepository
    {
        Task AddAsync(AdminRefreshTokenModel token);
        Task<AdminRefreshTokenModel?> GetByTokenHashAsync(string tokenHash);
        Task<IList<AdminRefreshTokenModel>> GetByUserIdAsync(string userId);
        Task RevokeAsync(AdminRefreshTokenModel token, string reason, string? revokedByIp = null, Guid? replacedById = null);
        Task RevokeTokenFamilyAsync(string userId, string reason);
        Task SaveChangesAsync();
    }
}

using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.Repositories
{
    public class AdminRefreshTokenRepository : IAdminRefreshTokenRepository
    {
        private readonly AdminPanelAppDbContext _context;

        public AdminRefreshTokenRepository(AdminPanelAppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AdminRefreshTokenModel token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task<AdminRefreshTokenModel?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        }

        public async Task<IList<AdminRefreshTokenModel>> GetByUserIdAsync(string userId)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task RevokeAsync(AdminRefreshTokenModel token, string reason, string? revokedByIp = null, Guid? replacedById = null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevocationReason = reason;
            token.RevokedByIp = revokedByIp;
            token.ReplacedByTokenId = replacedById;
            _context.RefreshTokens.Update(token);
        }

        public async Task RevokeTokenFamilyAsync(string userId, string reason)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevocationReason = reason;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

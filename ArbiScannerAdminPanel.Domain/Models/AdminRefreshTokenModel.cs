namespace ArbiScannerAdminPanel.Domain.Models
{
    public class AdminRefreshTokenModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public virtual AdminUserModel? User { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
        public Guid? ReplacedByTokenId { get; set; }
        public virtual AdminRefreshTokenModel? ReplacedByToken { get; set; }
        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }
        public string? UserAgent { get; set; }

        public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    }
}

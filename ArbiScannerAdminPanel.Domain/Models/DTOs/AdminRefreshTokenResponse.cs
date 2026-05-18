namespace ArbiScannerAdminPanel.Domain.Models.DTOs
{
    public class AdminRefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public long ExpiresIn { get; set; }
    }
}

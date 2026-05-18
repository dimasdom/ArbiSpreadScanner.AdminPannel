namespace ArbiScannerAdminPanel.Domain.Models.DTOs
{
    public class AdminAccountDTO
    {
        public string Token { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}

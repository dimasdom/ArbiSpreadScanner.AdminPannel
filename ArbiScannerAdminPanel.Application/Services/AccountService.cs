using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ArbiScannerAdminPanel.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AdminUserModel> _userManager;
        private readonly SignInManager<AdminUserModel> _signInManager;
        private readonly IAdminRefreshTokenRepository _refreshTokenRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<AdminUserModel> userManager,
            SignInManager<AdminUserModel> signInManager,
            IAdminRefreshTokenRepository refreshTokenRepository,
            IHttpContextAccessor httpContextAccessor,
            IOptions<JwtOptions> jwtOptions,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _refreshTokenRepository = refreshTokenRepository;
            _httpContextAccessor = httpContextAccessor;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<Result<AdminAccountDTO>> AuthenticateAdmin(AdminAccountAuthenticateDTO accountAuthenticateDTO)
        {
            var user = await _userManager.FindByNameAsync(accountAuthenticateDTO.UserName);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user {UserName} not found", accountAuthenticateDTO.UserName);
                return Result.Fail<AdminAccountDTO>(TypedErrors.Unauthorized("Invalid username or password"));
            }

            var res = await _signInManager.CheckPasswordSignInAsync(user, accountAuthenticateDTO.Password, false);
            if (!res.Succeeded)
            {
                _logger.LogWarning("Login failed: invalid password for user {UserName}", accountAuthenticateDTO.UserName);
                return Result.Fail<AdminAccountDTO>(TypedErrors.Unauthorized("Invalid username or password"));
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateJwt(user.Id, userRoles);

            var ipAddress = GetClientIp();
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            var refreshToken = await GenerateAndStoreRefreshToken(user.Id, ipAddress, userAgent);

            var dto = new AdminAccountDTO
            {
                AccessToken = accessToken,
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
                Roles = userRoles.ToList()
            };

            return Result.Ok(dto);
        }

        public async Task<Result<AdminRefreshTokenResponse>> RefreshAccessToken(string refreshToken, string? ipAddress = null, string? userAgent = null)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null)
            {
                _logger.LogWarning("Token refresh failed: refresh token not found");
                return Result.Fail<AdminRefreshTokenResponse>(TypedErrors.Unauthorized("Invalid refresh token"));
            }

            if (!storedToken.IsActive)
            {
                if (storedToken.RevokedAt != null)
                {
                    _logger.LogWarning("Suspicious refresh token reuse detected for user {UserId}", storedToken.UserId);
                    await _refreshTokenRepository.RevokeTokenFamilyAsync(storedToken.UserId, "suspicious_reuse");
                    await _refreshTokenRepository.SaveChangesAsync();
                }
                _logger.LogWarning("Token refresh failed: token is no longer active for user {UserId}", storedToken.UserId);
                return Result.Fail<AdminRefreshTokenResponse>(TypedErrors.Unauthorized("Refresh token is no longer valid"));
            }

            var user = storedToken.User;
            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: user not found for token {TokenId}", storedToken.Id);
                return Result.Fail<AdminRefreshTokenResponse>(TypedErrors.NotFound("User not found"));
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwt(user.Id, userRoles);
            var newRawRefreshToken = await GenerateAndStoreRefreshToken(user.Id, ipAddress, userAgent);

            var newHash = HashToken(newRawRefreshToken);
            var newStoredToken = await _refreshTokenRepository.GetByTokenHashAsync(newHash);
            await _refreshTokenRepository.RevokeAsync(storedToken, "token_reissued", ipAddress, newStoredToken?.Id);
            await _refreshTokenRepository.SaveChangesAsync();

            var expiresIn = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes).ToUnixTimeSeconds();
            return Result.Ok(new AdminRefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRawRefreshToken,
                ExpiresIn = expiresIn
            });
        }

        public async Task<Result> LogoutByToken(string userId, string refreshToken, string? ipAddress = null)
        {
            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null || storedToken.UserId != userId)
                return Result.Ok();

            await _refreshTokenRepository.RevokeAsync(storedToken, "logout", ipAddress);
            await _refreshTokenRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<AdminAccountDTO>> GetCurrentAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetCurrentAdmin failed: user {UserId} not found", userId);
                return Result.Fail<AdminAccountDTO>(TypedErrors.NotFound("User not found"));
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Result.Ok(new AdminAccountDTO
            {
                Roles = roles.ToList()
            });
        }

        private string GenerateJwt(string userId, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userId)
            };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, role));

            var claimsIdentity = new ClaimsIdentity(claims, "Token",
                ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                notBefore: now,
                claims: claimsIdentity.Claims,
                expires: now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtOptions.SigningKey)),
                    SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private async Task<string> GenerateAndStoreRefreshToken(string userId, string? ipAddress, string? userAgent)
        {
            var rawToken = GenerateRawToken();
            var tokenHash = HashToken(rawToken);

            var refreshTokenModel = new AdminRefreshTokenModel
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                CreatedByIp = ipAddress,
                UserAgent = userAgent
            };

            await _refreshTokenRepository.AddAsync(refreshTokenModel);
            await _refreshTokenRepository.SaveChangesAsync();

            return rawToken;
        }

        private static string GenerateRawToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        internal static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private string? GetClientIp()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
    }
}


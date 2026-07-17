using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArbiScannerAdminPanel.API.Controllers
{
    [ApiController]
    [Route("api/{controller}")]
    public class AccountController : ControllerBase
    {
        private const string AccessTokenCookieName = "adminpanel.access_token";
        private const string RefreshTokenCookieName = "adminpanel.refresh_token";

        private readonly IAccountService _accountService;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, IOptions<JwtOptions> jwtOptions, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("Authenticate")]
        public async Task<ActionResult<Result<AdminAccountDTO>>> Authenticate([FromBody] AdminAccountAuthenticateDTO accountAuthenticateDTO)
        {
            _logger.LogInformation("Authentication attempt for user {UserName}", accountAuthenticateDTO.UserName);
            var result = await _accountService.AuthenticateAdmin(accountAuthenticateDTO);
            if (result.IsSuccess)
            {
                AppendAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
                result.Value.AccessToken = string.Empty;
                result.Value.Token = string.Empty;
                result.Value.RefreshToken = string.Empty;
            }
            return result.ToSerializable();
        }

        [AllowAnonymous]
        [HttpPost("Refresh")]
        public async Task<ActionResult<Result<AdminRefreshTokenResponse>>> Refresh([FromBody] AdminRefreshTokenRequest? request)
        {
            var refreshToken = ResolveRefreshToken(request?.RefreshToken);
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(Result.Fail(TypedErrors.Validation("Refresh token is required.")).ToResult<AdminRefreshTokenResponse>().ToSerializable());

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _accountService.RefreshAccessToken(refreshToken, ipAddress, userAgent);
            if (result.IsSuccess)
            {
                AppendAuthCookies(result.Value.AccessToken, result.Value.RefreshToken);
                result.Value.AccessToken = string.Empty;
                result.Value.RefreshToken = string.Empty;
            }
            return result.ToSerializable();
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<ActionResult<Result>> Logout([FromBody] AdminRefreshTokenRequest? request)
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Result.Fail(TypedErrors.Unauthorized("User ID not found in token")).ToSerializable());

            var refreshToken = ResolveRefreshToken(request?.RefreshToken);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _accountService.LogoutByToken(userId, refreshToken, ipAddress);
            }

            ClearAuthCookies();
            return Result.Ok().ToSerializable();
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<ActionResult<Result<AdminAccountDTO>>> Me()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(Result.Fail(TypedErrors.Unauthorized("User ID not found in token")).ToResult<AdminAccountDTO>().ToSerializable());

            return (await _accountService.GetCurrentAdmin(userId)).ToSerializable();
        }

        private string? ResolveRefreshToken(string? fromBody)
        {
            if (!string.IsNullOrWhiteSpace(fromBody))
                return fromBody;

            return Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken)
                ? cookieToken
                : null;
        }

        private void AppendAuthCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append(AccessTokenCookieName, accessToken,
                CreateCookieOptions(TimeSpan.FromMinutes(_jwtOptions.AccessTokenExpirationMinutes)));
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken,
                CreateCookieOptions(TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationDays)));
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete(AccessTokenCookieName);
            Response.Cookies.Delete(RefreshTokenCookieName);
        }

        private CookieOptions CreateCookieOptions(TimeSpan lifetime)
        {
            var secure = Request.IsHttps;
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.Add(lifetime)
            };
        }
    }
}


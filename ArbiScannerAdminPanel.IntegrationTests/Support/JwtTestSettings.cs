using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ArbiScannerAdminPanel.IntegrationTests.Support;

// Fast, no-Keycloak auth path for most integration tests: forge a JWT signed with a
// known test key instead of validating against a real Authority/JWKS endpoint. See
// KeycloakDiscoveryTests for the one test that exercises real Authority-based
// discovery (and the real flat "role" claim protocol mapper) against a live
// Testcontainers.Keycloak instance.
internal static class JwtTestSettings
{
    public const string SigningKey = "integration-tests-signing-key-32-chars-minimum";
    public const string Issuer = "ArbiScannerAdminPanel.IntegrationTests";
    public const string Audience = "ArbiScannerAdminPanel.IntegrationTests";

    public static readonly Dictionary<string, string?> ConfigOverrides = new()
    {
        ["Jwt:Authority"] = string.Empty,
        ["Jwt:Audience"] = Audience,
    };

    // Overrides the JwtBearer options AddAuthenticationJwt configured from Jwt:Authority
    // with a static symmetric key, mirroring what a real Keycloak-issued token validates
    // against but without any network dependency. RoleClaimType = "role" is preserved
    // (matches production's AddAuthenticationJwt) so [Authorize(Roles = ...)] resolves
    // against the "role" claims JwtTestTokenFactory forges.
    public static void ConfigureTestJwtBearer(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = null;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SigningKey)),
                ValidateIssuerSigningKey = true,
                NameClaimType = "sub",
                RoleClaimType = "role",
            };
        });
    }
}

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArbiScannerAdminPanel.IntegrationTests.Support;

// Forges a JWT shaped like a real Keycloak-issued arbiscanner-admin access token
// (sub claim plus one "role" claim per role — matching the flat realm-role
// protocol mapper in keycloak/realm-export/arbiscanner-admin-realm.json), signed
// with JwtTestSettings' known test key.
internal static class JwtTestTokenFactory
{
    public static string CreateToken(string sub, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", sub) };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: JwtTestSettings.Issuer,
            audience: JwtTestSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtTestSettings.SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

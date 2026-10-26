using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FiscApi.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FiscApi.Authentication;

/// <summary>
/// Builds and signs a JWT for a user who just logged in. The API never stores tokens: it
/// trusts any token whose signature, issuer, audience and expiry check out (see Program.cs).
/// </summary>
public class JwtTokenService
{
    private readonly AuthenticationOptions _options;

    public JwtTokenService(IOptions<AuthenticationOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, int ExpiresInSeconds) GenerateToken(DemoUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuedAt = DateTime.UtcNow;

        // Claims = facts about the user carried inside the token. Never put secrets here:
        // a JWT is signed, not encrypted, so anyone can read it (try jwt.io).
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: issuedAt.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _options.AccessTokenMinutes * 60);
    }
}

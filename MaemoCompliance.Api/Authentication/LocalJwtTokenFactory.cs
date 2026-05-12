using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MaemoCompliance.Domain.Users;
using Microsoft.IdentityModel.Tokens;

namespace MaemoCompliance.Api.Authentication;

public sealed class LocalJwtTokenFactory(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user, Guid tenantId)
    {
        var key = configuration["Jwt:Key"]
                  ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        if (key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        }

        var issuer = configuration["Jwt:Issuer"] ?? "maemo-compliance.co.za";
        var audience = configuration["Jwt:Audience"] ?? "maemo-compliance.co.za";
        var hours = configuration.GetValue("Jwt:ExpiryHours", 8);
        var expires = DateTime.UtcNow.AddHours(hours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("tenant_id", tenantId.ToString()),
            new("auth_provider", user.AuthProvider),
            new(ClaimTypes.Role, ((int)user.Role).ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }
}

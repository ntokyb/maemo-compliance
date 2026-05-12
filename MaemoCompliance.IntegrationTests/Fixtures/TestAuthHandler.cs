using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaemoCompliance.IntegrationTests.Fixtures;

/// <summary>
/// Test authentication. Tenant resolution for API calls uses the X-Tenant-Id header (see TenantMiddleware);
/// NameIdentifier is a non-Guid user id so middleware does not treat the principal as API-key auth.
/// Optional header X-Test-Roles: comma-separated roles (default: TenantAdmin,DocumentApprover).
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "test-user-001"),
            new Claim(ClaimTypes.Name, "Integration Test User"),
        ];

        if (!Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader) ||
            string.IsNullOrWhiteSpace(rolesHeader))
        {
            claims.Add(new Claim(ClaimTypes.Role, "TenantAdmin"));
            claims.Add(new Claim(ClaimTypes.Role, "DocumentApprover"));
        }
        else
        {
            foreach (var role in rolesHeader.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

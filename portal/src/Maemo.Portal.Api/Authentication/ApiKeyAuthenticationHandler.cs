using Maemo.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Maemo.Portal.Api.Authentication;

/// <summary>
/// Authentication handler for API key authentication used by the Engine API.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Look for X-Api-Key header
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Validate the API key
        var apiKeyEntity = await _apiKeyService.ValidateAsync(apiKey, Context.RequestAborted);
        if (apiKeyEntity == null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Create claims principal
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKeyEntity.TenantId.ToString()),
            new Claim(ClaimTypes.Name, apiKeyEntity.Name ?? "API Key"),
            new Claim("ApiKeyId", apiKeyEntity.Id.ToString()),
            new Claim(ClaimTypes.Role, "ApiKeyClient")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = "ApiKey";
        return Task.CompletedTask;
    }
}


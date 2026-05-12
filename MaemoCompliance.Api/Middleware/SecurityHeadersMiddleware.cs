using MaemoCompliance.Application.Common;

namespace MaemoCompliance.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDeploymentContext _deploymentContext;

    public SecurityHeadersMiddleware(RequestDelegate next, IDeploymentContext deploymentContext)
    {
        _next = next;
        _deploymentContext = deploymentContext;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set standard security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");

        // Add Strict-Transport-Security for GovOnPrem deployments (assumes HTTPS)
        if (_deploymentContext.IsGovOnPrem)
        {
            // Max-age of 1 year (31536000 seconds)
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}


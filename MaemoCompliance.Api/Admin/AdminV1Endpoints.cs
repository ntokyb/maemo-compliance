using Microsoft.AspNetCore.Hosting;

namespace MaemoCompliance.Api.Admin;

/// <summary>
/// Admin V1 API endpoints coordinator - Internal platform operations for Codist staff.
/// All endpoints under /admin/v1 require PlatformAdmin authorization (except in Development).
/// </summary>
public static class AdminV1Endpoints
{
    /// <summary>
    /// Maps all Admin V1 endpoints under /admin/v1 route group.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminV1(this IEndpointRouteBuilder endpoints, IWebHostEnvironment? environment = null)
    {
        if (environment != null)
        {
            endpoints.MapAdminSeedEndpoints(environment);
        }

        // Create base admin group: /admin/v1
        var group = endpoints
            .MapGroup("/admin/v1")
            .WithTags("Admin");
        
        // Only require authorization in production
        // In development, allow anonymous access for local testing
        if (environment == null || !environment.IsDevelopment())
        {
            group.RequireAuthorization("PlatformAdmin"); // Requires PlatformAdmin role
        }

        // Dashboard + tenants + workers + billing + logs will be mapped in separate extension methods
        group.MapAdminDashboardEndpoints();
        group.MapAdminTenantEndpoints();
        group.MapAdminWorkersEndpoints();
        group.MapAdminBillingEndpoints();
        group.MapAdminLogsEndpoints();

        return endpoints;
    }
}


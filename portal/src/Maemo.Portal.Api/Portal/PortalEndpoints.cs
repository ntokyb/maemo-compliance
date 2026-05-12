namespace Maemo.Portal.Api.Portal;

/// <summary>
/// Portal API endpoints coordinator - Maps all Portal endpoints under /api route group.
/// </summary>
public static class PortalEndpoints
{
    /// <summary>
    /// Maps all Portal API endpoints under /api route group.
    /// </summary>
    public static WebApplication MapPortalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Portal");

        // Map all Portal endpoint modules
        app.MapDocumentsEndpoints();
        app.MapNcrsEndpoints();
        app.MapRisksEndpoints();
        app.MapAuditsEndpoints();
        app.MapDashboardEndpoints();
        app.MapConsultantsEndpoints();
        app.MapTenantsEndpoints();
        app.MapTenantSelfServiceEndpoints();
        app.MapBillingEndpoints();
        app.MapAuditLogEndpoints();
        app.MapDemoEndpoints();

        return app;
    }
}


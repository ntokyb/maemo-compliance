using MaemoCompliance.Engine.Api.Common;
using MaemoCompliance.Engine.Api.Engine.Audits;
using MaemoCompliance.Application.Tenants;

namespace MaemoCompliance.Engine.Api.Engine.Audits;

/// <summary>
/// Audit Engine API endpoints coordinator - Coordinates audit templates and runs endpoints.
/// </summary>
public static class AuditEngineEndpoints
{
    /// <summary>
    /// Maps all Audit Engine endpoints (templates and runs) under /engine/v1/audits route group.
    /// </summary>
    public static RouteGroupBuilder MapAuditEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        // Create the audits group once
        var auditsGroup = engineGroup
            .MapGroup("/audits")
            .WithTags("Engine - Audit")
            .WithDescription("Audit management - create templates, run audits, and track compliance assessments");

        // Check module access for all audit endpoints
        auditsGroup.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Audits"))
            {
                return ErrorResults.ModuleNotEnabled("Audits");
            }
            return await next(context);
        });

        // Map sub-modules
        auditsGroup.MapAuditTemplatesEngineEndpoints();
        auditsGroup.MapAuditRunsEngineEndpoints();

        return engineGroup;
    }
}


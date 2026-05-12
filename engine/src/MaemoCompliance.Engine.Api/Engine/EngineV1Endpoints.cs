using MaemoCompliance.Engine.Api.Engine.Audits;
using MaemoCompliance.Engine.Api.Engine.Consultants;
using MaemoCompliance.Engine.Api.Engine.Documents;
using MaemoCompliance.Engine.Api.Engine.Ncrs;
using MaemoCompliance.Engine.Api.Engine.Risks;
using MaemoCompliance.Engine.Api.Engine.Tenants;
using MaemoCompliance.Engine.Api.Engine.Webhooks;

namespace MaemoCompliance.Engine.Api.Engine;

/// <summary>
/// Engine V1 API endpoints - Versioned API surface for Maemo Compliance Engine.
/// All endpoints require authorization and are tenant-scoped.
/// </summary>
public static class EngineV1Endpoints
{
    /// <summary>
    /// Maps all Engine V1 endpoints under /engine/v1 route group.
    /// This is the API-first consumption layer for the Maemo Compliance Engine.
    /// All endpoints are tenant-scoped and support both JWT and API Key authentication.
    /// </summary>
    public static IEndpointRouteBuilder MapEngineV1(this IEndpointRouteBuilder endpoints)
    {
        // Create main route group with base path /engine/v1
        // Allow both JWT user auth and API Key auth via EngineClients policy
        var engineGroup = endpoints
            .MapGroup("/engine/v1")
            .WithTags("Engine V1")
            .WithDescription("Maemo Compliance Engine API - API-first consumption layer for external integrations")
            .RequireAuthorization("EngineClients");

        // Map sub-modules
        engineGroup.MapDocumentsEngineEndpoints();
        engineGroup.MapNcrEngineEndpoints();
        engineGroup.MapRisksEngineEndpoints();
        engineGroup.MapAuditEngineEndpoints();
        engineGroup.MapConsultantEngineEndpoints();
        engineGroup.MapTenantEngineEndpoints();
        engineGroup.MapWebhookEngineEndpoints();

        return endpoints;
    }
}

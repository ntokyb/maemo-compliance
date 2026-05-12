using MaemoCompliance.Application.Consultants.Dtos;
using MaemoCompliance.Application.Engine;

namespace MaemoCompliance.Engine.Api.Engine.Consultants;

/// <summary>
/// Consultants Engine API endpoints - Consultant-specific operations for the Compliance Engine.
/// </summary>
public static class ConsultantEngineEndpoints
{
    /// <summary>
    /// Maps all Consultants Engine endpoints under /engine/v1/consultants route group.
    /// </summary>
    public static RouteGroupBuilder MapConsultantEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var consultantsGroup = engineGroup
            .MapGroup("/consultants")
            .WithTags("Engine - Consultants")
            .WithDescription("Consultant-specific operations - dashboard summaries and client management")
            .RequireAuthorization("RequireConsultant");

        // GET /engine/v1/consultants/dashboard
        consultantsGroup.MapGet("/dashboard", async (
            IConsultantEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var summary = await engine.GetDashboardSummaryAsync(cancellationToken);
                return Results.Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetConsultantDashboard")
        .WithOpenApi()
        .Produces<ConsultantDashboardSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // GET /engine/v1/consultants/clients
        consultantsGroup.MapGet("/clients", async (
            IConsultantEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var clients = await engine.GetClientsAsync(cancellationToken);
                return Results.Ok(clients);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetConsultantClients")
        .WithOpenApi()
        .Produces<IReadOnlyList<ConsultantClientDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        return engineGroup;
    }
}


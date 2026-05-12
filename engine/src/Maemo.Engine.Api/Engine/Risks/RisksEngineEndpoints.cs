using Maemo.Engine.Api.Common;
using Maemo.Application.Engine;
using Maemo.Application.Ncrs.Dtos;
using Maemo.Application.Risks.Dtos;
using Maemo.Application.Tenants;
using Maemo.Domain.Risks;
using Maemo.Shared.Contracts.Engine.Risks;
using Microsoft.AspNetCore.Mvc;
using CreateRiskRequest = Maemo.Shared.Contracts.Engine.Risks.CreateRiskRequest;
using UpdateRiskRequest = Maemo.Shared.Contracts.Engine.Risks.UpdateRiskRequest;
using RiskFilter = Maemo.Shared.Contracts.Engine.Risks.RiskFilter;

namespace Maemo.Engine.Api.Engine.Risks;

/// <summary>
/// Risks Engine API endpoints - Risk Register management for the Compliance Engine.
/// </summary>
public static class RisksEngineEndpoints
{
    /// <summary>
    /// Maps all Risks Engine endpoints under /engine/v1/risks route group.
    /// </summary>
    public static RouteGroupBuilder MapRisksEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var risksGroup = engineGroup
            .MapGroup("/risks")
            .WithTags("Engine - Risks")
            .WithDescription("Risk Register management - identify, assess, and track compliance risks");

        // Check module access for all endpoints in this group
        risksGroup.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Risks"))
            {
                return ErrorResults.ModuleNotEnabled("Risks");
            }
            return await next(context);
        });

        // GET /engine/v1/risks
        risksGroup.MapGet("/", async (
            IRiskEngine engine,
            CancellationToken cancellationToken,
            [FromQuery] RiskCategory? category = null,
            [FromQuery] RiskStatus? status = null) =>
        {
            var filter = new RiskFilter
            {
                Category = category,
                Status = status
            };

            var risks = await engine.ListAsync(filter, cancellationToken);
            return Results.Ok(risks);
        })
        .WithName("EngineV1_GetRisks")
        .WithOpenApi()
        .Produces<IReadOnlyList<RiskDto>>(StatusCodes.Status200OK);

        // GET /engine/v1/risks/{id}
        risksGroup.MapGet("/{id:guid}", async (
            Guid id,
            IRiskEngine engine,
            CancellationToken cancellationToken) =>
        {
            var risk = await engine.GetAsync(id, cancellationToken);

            if (risk == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(risk);
        })
        .WithName("EngineV1_GetRiskById")
        .WithOpenApi()
        .Produces<RiskDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /engine/v1/risks
        risksGroup.MapPost("/", async (
            CreateRiskRequest request,
            IRiskEngine engine,
            CancellationToken cancellationToken) =>
        {
            var riskId = await engine.CreateAsync(request, cancellationToken);
            return Results.Created($"/engine/v1/risks/{riskId}", new { id = riskId });
        })
        .WithName("EngineV1_CreateRisk")
        .WithSummary("Create a new risk")
        .WithDescription("Creates a new risk entry in the risk register. Triggers a 'Risk.Created' webhook event if webhooks are configured.")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created, "application/json")
        .ProducesValidationProblem();

        // PUT /engine/v1/risks/{id}
        risksGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRiskRequest request,
            IRiskEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.UpdateAsync(id, request, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EngineV1_UpdateRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // GET /engine/v1/risks/{riskId}/ncrs
        risksGroup.MapGet("/{riskId:guid}/ncrs", async (
            Guid riskId,
            IRiskEngine engine,
            CancellationToken cancellationToken) =>
        {
            var ncrs = await engine.GetLinkedNcrsAsync(riskId, cancellationToken);
            return Results.Ok(ncrs);
        })
        .WithName("EngineV1_GetNcrsForRisk")
        .WithOpenApi()
        .Produces<IReadOnlyList<NcrDto>>(StatusCodes.Status200OK);

        return engineGroup;
    }
}


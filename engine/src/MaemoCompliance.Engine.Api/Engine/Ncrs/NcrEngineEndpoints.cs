using MaemoCompliance.Application.Common;
using MaemoCompliance.Engine.Api.Common;
using MaemoCompliance.Application.Engine;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Shared.Contracts.Engine.Ncrs;
using Microsoft.AspNetCore.Mvc;

namespace MaemoCompliance.Engine.Api.Engine.Ncrs;

/// <summary>
/// NCR Engine API endpoints - Non-Conformance Report management for the Compliance Engine.
/// </summary>
public static class NcrEngineEndpoints
{
    /// <summary>
    /// Maps all NCR Engine endpoints under /engine/v1/ncr route group.
    /// </summary>
    public static RouteGroupBuilder MapNcrEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var ncrGroup = engineGroup
            .MapGroup("/ncr")
            .WithTags("Engine - NCR")
            .WithDescription("Non-Conformance Report (NCR) management - track, manage, and resolve compliance issues");

        // Check module access for all endpoints in this group
        ncrGroup.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("NCR"))
            {
                return ErrorResults.ModuleNotEnabled("NCR");
            }
            return await next(context);
        });

        // GET /engine/v1/ncr
        ncrGroup.MapGet("/", async (
            INcrEngine engine,
            CancellationToken cancellationToken,
            [FromQuery] NcrStatus? status = null,
            [FromQuery] NcrSeverity? severity = null,
            [FromQuery] string? department = null) =>
        {
            var filter = new NcrFilter
            {
                Status = status,
                Severity = severity,
                Department = department
            };

            var ncrs = await engine.ListAsync(filter, cancellationToken);
            return Results.Ok(ncrs);
        })
        .WithName("EngineV1_GetNcrs")
        .WithSummary("List NCRs")
        .WithDescription("Retrieves a list of Non-Conformance Reports for the current tenant, optionally filtered by status, severity, and department")
        .WithOpenApi()
        .Produces<IReadOnlyList<NcrDto>>(StatusCodes.Status200OK);

        // GET /engine/v1/ncr/{id}
        ncrGroup.MapGet("/{id:guid}", async (
            Guid id,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            var ncr = await engine.GetAsync(id, cancellationToken);

            if (ncr == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ncr);
        })
        .WithName("EngineV1_GetNcrById")
        .WithOpenApi()
        .Produces<NcrDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /engine/v1/ncr
        ncrGroup.MapPost("/", async (
            CreateNcrRequest request,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            var ncrId = await engine.CreateAsync(request, cancellationToken);
            return Results.Created($"/engine/v1/ncr/{ncrId}", new { id = ncrId });
        })
        .WithName("EngineV1_CreateNcr")
        .WithSummary("Create a new NCR")
        .WithDescription("Creates a new Non-Conformance Report. Triggers a 'NCR.Created' webhook event if webhooks are configured.")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created, "application/json")
        .ProducesValidationProblem();

        // PUT /engine/v1/ncr/{id}
        ncrGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateNcrRequest request,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var ncr = await engine.UpdateAsync(id, request, cancellationToken);
                return Results.Ok(ncr);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EngineV1_UpdateNcr")
        .WithSummary("Update an NCR")
        .WithDescription("Updates editable fields of an NCR. Status changes use PUT /ncr/{id}/status.")
        .WithOpenApi()
        .Produces<NcrDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // DELETE /engine/v1/ncr/{id}
        ncrGroup.MapDelete("/{id:guid}", async (
            Guid id,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.DeleteAsync(id, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ConflictException ex)
            {
                return ErrorResults.Conflict("NcrClosed", ex.Message);
            }
        })
        .WithName("EngineV1_DeleteNcr")
        .WithSummary("Delete an NCR")
        .WithDescription("Deletes an NCR and its risk links and status history. Closed NCRs cannot be deleted.")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // PUT /engine/v1/ncr/{id}/status
        ncrGroup.MapPut("/{id:guid}/status", async (
            Guid id,
            UpdateNcrStatusRequest request,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.UpdateStatusAsync(id, request.Status, request.DueDate, request.ClosedAt, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EngineV1_UpdateNcrStatus")
        .WithSummary("Update NCR status")
        .WithDescription("Updates the status of an NCR (e.g., Open → InProgress → Closed). Triggers a 'NCR.StatusChanged' webhook event if webhooks are configured.")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // GET /engine/v1/ncr/{id}/history
        ncrGroup.MapGet("/{id:guid}/history", async (
            Guid id,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            var history = await engine.GetHistoryAsync(id, cancellationToken);
            return Results.Ok(history);
        })
        .WithName("EngineV1_GetNcrHistory")
        .WithOpenApi()
        .Produces<IReadOnlyList<NcrStatusHistoryDto>>(StatusCodes.Status200OK);

        // POST /engine/v1/ncr/{ncrId}/risks/{riskId}
        ncrGroup.MapPost("/{ncrId:guid}/risks/{riskId:guid}", async (
            Guid ncrId,
            Guid riskId,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.LinkToRiskAsync(ncrId, riskId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_LinkNcrToRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /engine/v1/ncr/{ncrId}/risks/{riskId}
        ncrGroup.MapDelete("/{ncrId:guid}/risks/{riskId:guid}", async (
            Guid ncrId,
            Guid riskId,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            await engine.UnlinkFromRiskAsync(ncrId, riskId, cancellationToken);
            return Results.NoContent();
        })
        .WithName("EngineV1_UnlinkNcrFromRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        // GET /engine/v1/ncr/{ncrId}/risks
        ncrGroup.MapGet("/{ncrId:guid}/risks", async (
            Guid ncrId,
            INcrEngine engine,
            CancellationToken cancellationToken) =>
        {
            var risks = await engine.GetLinkedRisksAsync(ncrId, cancellationToken);
            return Results.Ok(risks);
        })
        .WithName("EngineV1_GetRisksForNcr")
        .WithOpenApi()
        .Produces<IReadOnlyList<RiskDto>>(StatusCodes.Status200OK);

        return engineGroup;
    }
}


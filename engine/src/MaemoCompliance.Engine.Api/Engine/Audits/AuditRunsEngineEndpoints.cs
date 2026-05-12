using MaemoCompliance.Engine.Api.Common;
using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Engine;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Shared.Contracts.Engine.Audits;
using MediatR;

namespace MaemoCompliance.Engine.Api.Engine.Audits;

/// <summary>
/// Audit Runs Engine API endpoints - Audit run management for the Compliance Engine.
/// </summary>
public static class AuditRunsEngineEndpoints
{
    /// <summary>
    /// Maps all Audit Runs Engine endpoints under /engine/v1/audits/runs route group.
    /// </summary>
    public static RouteGroupBuilder MapAuditRunsEngineEndpoints(this RouteGroupBuilder auditsGroup)
    {
        // GET /engine/v1/audits/runs
        auditsGroup.MapGet("/runs", async (
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var runs = await engine.GetRunsAsync(cancellationToken);
                return Results.Ok(runs);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetAuditRuns")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditRunDto>>(StatusCodes.Status200OK);

        // POST /engine/v1/audits/runs
        auditsGroup.MapPost("/runs", async (
            StartAuditRunRequest request,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var runId = await engine.StartRunAsync(request, cancellationToken);
                return Results.Created($"/engine/v1/audits/runs/{runId}", new { id = runId });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_StartAuditRun")
        .WithSummary("Start a new audit run")
        .WithDescription("Starts a new audit run based on an audit template. Triggers an 'Audit.Started' webhook event if webhooks are configured.")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /engine/v1/audits/runs/{id}/answers
        auditsGroup.MapGet("/runs/{id:guid}/answers", async (
            Guid id,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var answers = await engine.GetRunAnswersAsync(id, cancellationToken);
                return Results.Ok(answers);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetAuditAnswers")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditAnswerDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /engine/v1/audits/runs/{id}/answers
        auditsGroup.MapPost("/runs/{id:guid}/answers", async (
            Guid id,
            SubmitAuditAnswerRequest request,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.SubmitAnswerAsync(id, request, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_SubmitAuditAnswer")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /engine/v1/audits/runs/{runId}/questions/{questionId}/evidence
        auditsGroup.MapPost("/runs/{runId:guid}/questions/{questionId:guid}/evidence", async (
            Guid runId,
            Guid questionId,
            IFormFile file,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No file provided." });
            }

            try
            {
                using var fileStream = file.OpenReadStream();
                var command = new UploadAuditEvidenceCommand
                {
                    AuditRunId = runId,
                    AuditQuestionId = questionId,
                    FileName = file.FileName,
                    FileContent = fileStream
                };

                var evidenceFileUrl = await mediator.Send(command, cancellationToken);
                return Results.Ok(new { evidenceFileUrl });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_UploadAuditEvidence")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();

        return auditsGroup;
    }
}


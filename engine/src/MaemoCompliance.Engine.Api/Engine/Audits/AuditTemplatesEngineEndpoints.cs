using MaemoCompliance.Engine.Api.Common;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Engine;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Shared.Contracts.Engine.Audits;
using MediatR;

namespace MaemoCompliance.Engine.Api.Engine.Audits;

/// <summary>
/// Audit Templates Engine API endpoints - Template management for the Compliance Engine.
/// </summary>
public static class AuditTemplatesEngineEndpoints
{
    /// <summary>
    /// Maps all Audit Templates Engine endpoints under /engine/v1/audits/templates route group.
    /// </summary>
    public static RouteGroupBuilder MapAuditTemplatesEngineEndpoints(this RouteGroupBuilder auditsGroup)
    {
        // GET /engine/v1/audits/templates
        auditsGroup.MapGet("/templates", async (
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var templates = await engine.GetTemplatesAsync(cancellationToken);
                return Results.Ok(templates);
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
        .WithName("EngineV1_GetAuditTemplates")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditTemplateDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // POST /engine/v1/audits/templates
        auditsGroup.MapPost("/templates", async (
            CreateAuditTemplateRequest request,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var templateId = await engine.CreateTemplateAsync(request, cancellationToken);
                return Results.Created($"/engine/v1/audits/templates/{templateId}", new { id = templateId });
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
        .WithName("EngineV1_CreateAuditTemplate")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /engine/v1/audits/templates/{id}/questions
        auditsGroup.MapGet("/templates/{id:guid}/questions", async (
            Guid id,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var questions = await engine.GetTemplateQuestionsAsync(id, cancellationToken);
                return Results.Ok(questions);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
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
        .WithName("EngineV1_GetAuditQuestions")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditQuestionDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // POST /engine/v1/audits/templates/{id}/questions
        auditsGroup.MapPost("/templates/{id:guid}/questions", async (
            Guid id,
            AddAuditQuestionRequest request,
            IAuditEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var questionId = await engine.AddQuestionToTemplateAsync(id, request, cancellationToken);
                return Results.Created($"/engine/v1/audits/questions/{questionId}", new { id = questionId });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
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
        .WithName("EngineV1_AddAuditQuestion")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        return auditsGroup;
    }
}


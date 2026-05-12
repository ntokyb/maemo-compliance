using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Audits.Queries;
using MaemoCompliance.Application.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace MaemoCompliance.Api.Portal;

public static class AuditsEndpoints
{
    public static void MapAuditsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audits")
            .WithTags("Audits");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // Check module access for all endpoints in this group
        group.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Audits"))
            {
                return ErrorResults.ModuleNotEnabled("Audits");
            }
            return await next(context);
        });

        // GET /api/audits/templates
        // List audit templates (Consultant-only)
        group.MapGet("/templates", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetAuditTemplatesQuery();
                var templates = await mediator.Send(query, cancellationToken);
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
        .WithName("GetAuditTemplates")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditTemplateDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);

        // POST /api/audits/templates
        // Create audit template (Consultant-only)
        group.MapPost("/templates", async (
            CreateAuditTemplateCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var templateId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/audits/templates/{templateId}", new { id = templateId });
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
        .WithName("CreateAuditTemplate")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/audits/templates/{id}/questions
        // Get questions for a template
        group.MapGet("/templates/{id:guid}/questions", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetAuditQuestionsQuery { AuditTemplateId = id };
                var questions = await mediator.Send(query, cancellationToken);
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
        .WithName("GetAuditQuestions")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditQuestionDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden);

        // POST /api/audits/templates/{id}/questions
        // Add question to template (Consultant-only)
        group.MapPost("/templates/{id:guid}/questions", async (
            Guid id,
            AddAuditQuestionCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                command.AuditTemplateId = id;
                var questionId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/audits/questions/{questionId}", new { id = questionId });
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
        .WithName("AddAuditQuestion")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/audits/runs
        // List audit runs for current tenant
        group.MapGet("/runs", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetAuditRunsQuery();
                var runs = await mediator.Send(query, cancellationToken);
                return Results.Ok(runs);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetAuditRuns")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditRunDto>>(StatusCodes.Status200OK);

        // POST /api/audits/runs
        // Start new audit run
        group.MapPost("/runs", async (
            StartAuditRunCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var runId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/audits/runs/{runId}", new { id = runId });
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
        .WithName("StartAuditRun")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/audits/runs/{id}
        group.MapGet("/runs/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetAuditRunByIdQuery { Id = id };
                var run = await mediator.Send(query, cancellationToken);
                return Results.Ok(run);
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
        .WithName("GetAuditRunById")
        .WithOpenApi()
        .Produces<AuditRunDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/audits/runs/{id}/complete
        group.MapPost("/runs/{id:guid}/complete", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var dto = await mediator.Send(new CompleteAuditRunCommand { AuditRunId = id }, cancellationToken);
                return Results.Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ConflictException ex)
            {
                return ErrorResults.Conflict("AuditRunCompleteConflict", ex.Message);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CompleteAuditRun")
        .WithOpenApi()
        .Produces<AuditRunDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/audits/runs/{id}/answers
        // Get answers for an audit run
        group.MapGet("/runs/{id:guid}/answers", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetAuditAnswersQuery { AuditRunId = id };
                var answers = await mediator.Send(query, cancellationToken);
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
        .WithName("GetAuditAnswers")
        .WithOpenApi()
        .Produces<IReadOnlyList<AuditAnswerDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/audits/runs/{id}/answers
        // Submit answer for an audit run
        group.MapPost("/runs/{id:guid}/answers", async (
            Guid id,
            SubmitAuditAnswerCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                command.AuditRunId = id;
                await mediator.Send(command, cancellationToken);
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
            catch (ConflictException ex)
            {
                return ErrorResults.Conflict("AuditRunReadOnly", ex.Message);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("SubmitAuditAnswer")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/audits/runs/{runId}/questions/{questionId}/evidence
        // Upload evidence file for an audit answer
        group.MapPost("/runs/{runId:guid}/questions/{questionId:guid}/evidence", async (
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
            catch (ConflictException ex)
            {
                return ErrorResults.Conflict("AuditRunReadOnly", ex.Message);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UploadAuditEvidence")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);
    }
}


using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Commands;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Ncrs.Queries;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Domain.Ncrs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaemoCompliance.Api.Portal;

public static class NcrsEndpoints
{
    public static void MapNcrsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ncrs")
            .WithTags("NCRs");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // Check module access for all endpoints in this group
        group.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("NCR"))
            {
                return ErrorResults.ModuleNotEnabled("NCR");
            }
            return await next(context);
        });

        // GET /api/ncrs
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] NcrStatus? status = null,
            [FromQuery] NcrSeverity? severity = null,
            [FromQuery] string? department = null) =>
        {
            var query = new GetNcrListQuery
            {
                Status = status,
                Severity = severity,
                Department = department
            };

            var ncrs = await mediator.Send(query, cancellationToken);
            return Results.Ok(ncrs);
        })
        .WithName("GetNcrs")
        .WithOpenApi()
        .Produces<IReadOnlyList<NcrDto>>(StatusCodes.Status200OK);

        // GET /api/ncrs/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNcrByIdQuery { Id = id };
            var ncr = await mediator.Send(query, cancellationToken);

            if (ncr == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ncr);
        })
        .WithName("GetNcrById")
        .WithOpenApi()
        .Produces<NcrDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/ncrs
        group.MapPost("/", async (
            CreateNcrCommand request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateNcrCommand
            {
                Title = request.Title,
                Description = request.Description,
                Department = request.Department,
                OwnerUserId = request.OwnerUserId,
                Severity = request.Severity,
                DueDate = request.DueDate,
                Category = request.Category,
                RootCause = request.RootCause,
                CorrectiveAction = request.CorrectiveAction,
                EscalationLevel = request.EscalationLevel
            };

            var ncrId = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/ncrs/{ncrId}", new { id = ncrId });
        })
        .WithName("CreateNcr")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/ncrs/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateNcrRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateNcrCommand
            {
                NcrId = id,
                Request = request
            };

            try
            {
                var ncr = await mediator.Send(command, cancellationToken);
                return Results.Ok(ncr);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateNcr")
        .WithOpenApi()
        .Produces<NcrDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // DELETE /api/ncrs/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeleteNcrCommand { NcrId = id };
                await mediator.Send(command, cancellationToken);
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
        .WithName("DeleteNcr")
        .RequireAuthorization("RequireTenantAdmin")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);

        // PUT /api/ncrs/{id}/status
        group.MapPut("/{id:guid}/status", async (
            Guid id,
            UpdateNcrStatusRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateNcrStatusCommand
            {
                Id = id,
                Status = request.Status,
                DueDate = request.DueDate,
                ClosedAt = request.ClosedAt
            };

            try
            {
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateNcrStatus")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // GET /api/ncrs/{id}/history
        group.MapGet("/{id:guid}/history", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNcrHistoryQuery { NcrId = id };
            var history = await mediator.Send(query, cancellationToken);
            return Results.Ok(history);
        })
        .WithName("GetNcrHistory")
        .WithOpenApi()
        .Produces<IReadOnlyList<NcrStatusHistoryDto>>(StatusCodes.Status200OK);

        // POST /api/ncrs/{ncrId}/risks/{riskId}
        group.MapPost("/{ncrId:guid}/risks/{riskId:guid}", async (
            Guid ncrId,
            Guid riskId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new LinkNcrToRiskCommand
            {
                NcrId = ncrId,
                RiskId = riskId
            };

            try
            {
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("LinkNcrToRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /api/ncrs/{ncrId}/risks/{riskId}
        group.MapDelete("/{ncrId:guid}/risks/{riskId:guid}", async (
            Guid ncrId,
            Guid riskId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UnlinkNcrFromRiskCommand
            {
                NcrId = ncrId,
                RiskId = riskId
            };

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        })
        .WithName("UnlinkNcrFromRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        // GET /api/ncrs/{ncrId}/risks
        group.MapGet("/{ncrId:guid}/risks", async (
            Guid ncrId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRisksForNcrQuery { NcrId = ncrId };
            var risks = await mediator.Send(query, cancellationToken);
            return Results.Ok(risks);
        })
        .WithName("GetRisksForNcr")
        .WithOpenApi()
        .Produces<IReadOnlyList<MaemoCompliance.Application.Risks.Dtos.RiskDto>>(StatusCodes.Status200OK);
    }
}

public class UpdateNcrStatusRequest
{
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
}


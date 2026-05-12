using Maemo.Portal.Api.Common;
using Maemo.Application.Risks.Commands;
using Maemo.Application.Risks.Dtos;
using Maemo.Application.Risks.Queries;
using Maemo.Application.Tenants;
using Maemo.Domain.Risks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Portal.Api.Portal;

public static class RisksEndpoints
{
    public static void MapRisksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/risks")
            .WithTags("Risks");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // Check module access for all endpoints in this group
        group.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Risks"))
            {
                return ErrorResults.ModuleNotEnabled("Risks");
            }
            return await next(context);
        });

        // GET /api/risks
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] RiskCategory? category = null,
            [FromQuery] RiskStatus? status = null) =>
        {
            var query = new GetRisksQuery
            {
                Category = category,
                Status = status
            };

            var risks = await mediator.Send(query, cancellationToken);
            return Results.Ok(risks);
        })
        .WithName("GetRisks")
        .WithOpenApi()
        .Produces<IReadOnlyList<RiskDto>>(StatusCodes.Status200OK);

        // GET /api/risks/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRiskByIdQuery { Id = id };
            var risk = await mediator.Send(query, cancellationToken);

            if (risk == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(risk);
        })
        .WithName("GetRiskById")
        .WithOpenApi()
        .Produces<RiskDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/risks
        group.MapPost("/", async (
            CreateRiskRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateRiskCommand
            {
                Request = request
            };

            var riskId = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/risks/{riskId}", new { id = riskId });
        })
        .WithName("CreateRisk")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/risks/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRiskRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateRiskCommand
            {
                Id = id,
                Request = request
            };

            try
            {
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateRisk")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // GET /api/risks/{riskId}/ncrs
        group.MapGet("/{riskId:guid}/ncrs", async (
            Guid riskId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new Maemo.Application.Ncrs.Queries.GetNcrsForRiskQuery { RiskId = riskId };
            var ncrs = await mediator.Send(query, cancellationToken);
            return Results.Ok(ncrs);
        })
        .WithName("GetNcrsForRisk")
        .WithOpenApi()
        .Produces<IReadOnlyList<Maemo.Application.Ncrs.Dtos.NcrDto>>(StatusCodes.Status200OK);
    }
}


using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Audits.Queries;
using MaemoCompliance.Application.Tenants;
using MediatR;

namespace MaemoCompliance.Api.Portal;

public static class AuditProgrammesEndpoints
{
    public static void MapAuditProgrammesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audit-programmes")
            .WithTags("Audit Programmes");

        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        group.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Audits"))
            {
                return ErrorResults.ModuleNotEnabled("Audits");
            }
            return await next(context);
        });

        group.MapPost("/", async (
            CreateAuditProgrammeCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var id = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/audit-programmes/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("AuditProgrammeCreateFailed", ex.Message);
            }
        })
        .WithName("CreateAuditProgramme")
        .WithOpenApi()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{year:int}", async (
            int year,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetAuditProgrammeByYearQuery { Year = year }, cancellationToken);
            if (dto == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(dto);
        })
        .WithName("GetAuditProgrammeByYear")
        .WithOpenApi()
        .Produces<AuditProgrammeDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{programmeId:guid}/items/{itemId:guid}/link-audit", async (
            Guid programmeId,
            Guid itemId,
            LinkAuditRequest body,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await mediator.Send(new LinkAuditToScheduleItemCommand
                {
                    ProgrammeId = programmeId,
                    ItemId = itemId,
                    AuditId = body.AuditId,
                }, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("LinkAuditToScheduleItem")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }

    public sealed record LinkAuditRequest(Guid AuditId);
}

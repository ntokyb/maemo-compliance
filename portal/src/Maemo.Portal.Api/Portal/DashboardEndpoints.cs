using Maemo.Application.Dashboard;
using Maemo.Application.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Portal.Api.Portal;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // GET /api/dashboard/summary
        group.MapGet("/summary", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardSummaryQuery();
            var summary = await mediator.Send(query, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("GetDashboardSummary")
        .WithOpenApi()
        .Produces<DashboardSummaryDto>(StatusCodes.Status200OK);

        // GET /api/dashboard/popia-personal-info-documents
        group.MapGet("/popia-personal-info-documents", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPopiaPersonalInfoDocumentsQuery();
            var documents = await mediator.Send(query, cancellationToken);
            return Results.Ok(documents);
        })
        .WithName("GetPopiaPersonalInfoDocuments")
        .WithOpenApi()
        .Produces<IReadOnlyList<Maemo.Application.Documents.Dtos.DocumentDto>>(StatusCodes.Status200OK);

        // GET /api/dashboard/documents-near-retention-expiry
        group.MapGet("/documents-near-retention-expiry", async (
            IMediator mediator,
            [FromQuery] int? daysAhead,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDocumentsNearRetentionExpiryQuery
            {
                DaysAhead = daysAhead ?? 90
            };
            var documents = await mediator.Send(query, cancellationToken);
            return Results.Ok(documents);
        })
        .WithName("GetDocumentsNearRetentionExpiry")
        .WithOpenApi()
        .Produces<IReadOnlyList<Maemo.Application.Documents.Dtos.DocumentDto>>(StatusCodes.Status200OK);
    }
}


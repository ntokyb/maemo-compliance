using Maemo.Admin.Api.Common;
using Maemo.Application.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Admin.Api.Admin;

public static class AdminPopiaEndpoints
{
    public static IEndpointRouteBuilder MapAdminPopiaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/popia")
            .WithTags("POPIA");

        // GET /admin/v1/popia/documents/summary
        group.MapGet("/documents/summary", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetPopiaDocumentSummaryQuery();
                var summary = await mediator.Send(query, cancellationToken);
                return Results.Ok(summary);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get POPIA document summary: {ex.Message}");
            }
        })
        .WithName("GetPopiaDocumentSummary")
        .WithOpenApi()
        .Produces<Maemo.Application.Documents.Dtos.PopiaDocumentSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}


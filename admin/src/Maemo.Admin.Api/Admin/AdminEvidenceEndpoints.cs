using Maemo.Admin.Api.Common;
using Maemo.Application.Evidence.Queries;
using MediatR;

namespace Maemo.Admin.Api.Admin;

public static class AdminEvidenceEndpoints
{
    public static IEndpointRouteBuilder MapAdminEvidenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/evidence")
            .WithTags("Evidence");

        // GET /admin/v1/evidence
        group.MapGet("", async (
            Guid? tenantId,
            string? entityType,
            DateTime? fromDate,
            DateTime? toDate,
            int? limit,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetEvidenceRegisterQuery
                {
                    TenantId = tenantId,
                    EntityType = entityType,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Limit = limit ?? 100
                };

                var results = await mediator.Send(query, cancellationToken);
                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get evidence register: {ex.Message}");
            }
        })
        .WithName("GetEvidenceRegister")
        .WithOpenApi()
        .Produces<IReadOnlyList<Maemo.Application.Evidence.Dtos.EvidenceItemDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}


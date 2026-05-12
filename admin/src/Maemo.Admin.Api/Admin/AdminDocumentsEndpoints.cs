using Maemo.Admin.Api.Common;
using Maemo.Application.Documents.Commands;
using MediatR;

namespace Maemo.Admin.Api.Admin;

public static class AdminDocumentsEndpoints
{
    public static IEndpointRouteBuilder MapAdminDocumentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/documents")
            .WithTags("Documents");

        // POST /admin/v1/documents/{id:guid}/destroy
        group.MapPost("/{id:guid}/destroy", async (
            Guid id,
            DestroyDocumentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DestroyDocumentCommand
                {
                    DocumentId = id,
                    Reason = request.Reason
                };

                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("DestructionError", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("DestructionError", $"Failed to destroy document: {ex.Message}");
            }
        })
        .WithName("DestroyDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

public record DestroyDocumentRequest
{
    public string Reason { get; set; } = null!;
}


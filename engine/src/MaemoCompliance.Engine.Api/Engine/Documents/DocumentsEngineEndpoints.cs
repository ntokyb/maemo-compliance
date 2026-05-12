using MaemoCompliance.Engine.Api.Common;
using MaemoCompliance.Application.Documents.Commands;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Application.Documents.Queries;
using MaemoCompliance.Application.Engine;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ChangeDocumentStatusRequest = MaemoCompliance.Shared.Contracts.Engine.Documents.ChangeDocumentStatusRequest;
using DownloadDocumentFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentFileQuery;
using DeleteDocumentCommand = MaemoCompliance.Application.Documents.Commands.DeleteDocumentCommand;
using GetDocumentVersionsQuery = MaemoCompliance.Application.Documents.Queries.GetDocumentVersionsQuery;
using CreateDocumentVersionCommand = MaemoCompliance.Application.Documents.Commands.CreateDocumentVersionCommand;
using DownloadDocumentVersionFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentVersionFileQuery;
using DocumentVersionDto = MaemoCompliance.Application.Documents.Dtos.DocumentVersionDto;

namespace MaemoCompliance.Engine.Api.Engine.Documents;

/// <summary>
/// Documents Engine API endpoints - Document lifecycle management for the Compliance Engine.
/// </summary>
public static class DocumentsEngineEndpoints
{
    /// <summary>
    /// Maps all Documents Engine endpoints under /engine/v1/documents route group.
    /// </summary>
    public static RouteGroupBuilder MapDocumentsEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var documentsGroup = engineGroup
            .MapGroup("/documents")
            .WithTags("Engine - Documents")
            .WithDescription("Document lifecycle management - create, update, version, and track compliance documents");

        // Check module access for all endpoints in this group
        documentsGroup.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Documents"))
            {
                return ErrorResults.ModuleNotEnabled("Documents");
            }
            return await next(context);
        });

        // GET /engine/v1/documents
        documentsGroup.MapGet("/", async (
            IDocumentsEngine engine,
            CancellationToken cancellationToken,
            [FromQuery] DocumentStatus? status = null,
            [FromQuery] string? department = null,
            [FromQuery] bool includeAllVersions = false) =>
        {
            var filter = new DocumentFilter
            {
                Status = status,
                Department = department,
                IncludeAllVersions = includeAllVersions
            };

            var documents = await engine.ListAsync(filter, cancellationToken);
            return Results.Ok(documents);
        })
        .WithName("EngineV1_GetDocuments")
        .WithSummary("List documents")
        .WithDescription("Retrieves a list of documents for the current tenant, optionally filtered by status and department")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK);

        // GET /engine/v1/documents/{id}
        documentsGroup.MapGet("/{id:guid}", async (
            Guid id,
            IDocumentsEngine engine,
            CancellationToken cancellationToken) =>
        {
            var document = await engine.GetAsync(id, cancellationToken);

            if (document == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(document);
        })
        .WithName("EngineV1_GetDocumentById")
        .WithSummary("Get document by ID")
        .WithDescription("Retrieves a specific document by its unique identifier")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /engine/v1/documents
        documentsGroup.MapPost("/", async (
            CreateDocumentRequest request,
            IDocumentsEngine engine,
            CancellationToken cancellationToken) =>
        {
            var documentId = await engine.CreateAsync(request, cancellationToken);
            return Results.Created($"/engine/v1/documents/{documentId}", new { id = documentId });
        })
        .WithName("EngineV1_CreateDocument")
        .WithSummary("Create a new document")
        .WithDescription("Creates a new compliance document. Triggers a 'Document.Created' webhook event if webhooks are configured.")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created, "application/json")
        .ProducesValidationProblem();

        // PUT /engine/v1/documents/{id}
        documentsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDocumentRequest request,
            IDocumentsEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.UpdateAsync(id, request, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EngineV1_UpdateDocument")
        .WithSummary("Update an existing document")
        .WithDescription("Updates the properties of an existing document")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // PUT /engine/v1/documents/{id}/status
        documentsGroup.MapPut("/{id:guid}/status", async (
            Guid id,
            ChangeDocumentStatusRequest request,
            IDocumentsEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.ChangeStatusAsync(id, request.Status, request.ApproverUserId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_ChangeDocumentStatus")
        .WithSummary("Change document status")
        .WithDescription("Updates the status of a document (e.g., Draft → Active → Archived)")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /engine/v1/documents/{id}/upload
        documentsGroup.MapPost("/{id:guid}/upload", async (
            Guid id,
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
                var command = new UploadDocumentFileCommand
                {
                    DocumentId = id,
                    FileName = file.FileName,
                    FileContent = fileStream
                };

                var storageLocation = await mediator.Send(command, cancellationToken);
                return Results.Ok(new { storageLocation });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_UploadDocumentFile")
        .WithOpenApi()
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();

        // GET /engine/v1/documents/{id}/download
        documentsGroup.MapGet("/{id:guid}/download", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new DownloadDocumentFileQuery { DocumentId = id };
                var fileStream = await mediator.Send(query, cancellationToken);

                if (fileStream == null)
                {
                    return ErrorResults.NotFound("FileNotFound", "Document file not found or document has no associated file.");
                }

                // Get document to determine file name
                var documentQuery = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(documentQuery, cancellationToken);

                var fileName = document?.Title ?? "document";
                if (!string.IsNullOrWhiteSpace(document?.StorageLocation))
                {
                    var lastSlash = document.StorageLocation.LastIndexOf('/');
                    if (lastSlash >= 0 && lastSlash < document.StorageLocation.Length - 1)
                    {
                        fileName = document.StorageLocation.Substring(lastSlash + 1);
                    }
                }

                return Results.File(fileStream, "application/octet-stream", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidOperation", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("DownloadError", $"Failed to download file: {ex.Message}");
            }
        })
        .WithName("EngineV1_DownloadDocumentFile")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /engine/v1/documents/{id}
        documentsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new DeleteDocumentCommand { DocumentId = id };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("DeleteError", $"Failed to delete document: {ex.Message}");
            }
        })
        .WithName("EngineV1_DeleteDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /engine/v1/documents/{id}/versions
        documentsGroup.MapGet("/{id:guid}/versions", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetDocumentVersionsQuery { DocumentId = id };
                var versions = await mediator.Send(query, cancellationToken);
                return Results.Ok(versions);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get document versions: {ex.Message}");
            }
        })
        .WithName("EngineV1_GetDocumentVersions")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentVersionDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /engine/v1/documents/{id}/versions
        documentsGroup.MapPost("/{id:guid}/versions", async (
            Guid id,
            IFormFile file,
            [FromForm] string? comment,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return ErrorResults.BadRequest("InvalidFile", "No file provided.");
            }

            try
            {
                using var fileStream = file.OpenReadStream();
                var command = new CreateDocumentVersionCommand
                {
                    DocumentId = id,
                    FileContent = fileStream,
                    FileName = file.FileName,
                    Comment = comment
                };

                var versionId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/engine/v1/documents/{id}/versions/{versionId}", new { id = versionId });
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("VersionError", $"Failed to create document version: {ex.Message}");
            }
        })
        .WithName("EngineV1_CreateDocumentVersion")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();

        // GET /engine/v1/documents/{id}/versions/{version}/download
        documentsGroup.MapGet("/{id:guid}/versions/{version:int}/download", async (
            Guid id,
            int version,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new DownloadDocumentVersionFileQuery { DocumentId = id, VersionNumber = version };
                var fileStream = await mediator.Send(query, cancellationToken);

                if (fileStream == null)
                {
                    return ErrorResults.NotFound("VersionNotFound", "Document version file not found.");
                }

                // Get version metadata for file name
                var versionsQuery = new GetDocumentVersionsQuery { DocumentId = id };
                var versions = await mediator.Send(versionsQuery, cancellationToken);
                var versionInfo = versions.FirstOrDefault(v => v.VersionNumber == version);

                var fileName = versionInfo?.FileName ?? $"document-v{version}";
                return Results.File(fileStream, "application/octet-stream", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("DownloadError", $"Failed to download version file: {ex.Message}");
            }
        })
        .WithName("EngineV1_DownloadDocumentVersionFile")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        return engineGroup;
    }
}


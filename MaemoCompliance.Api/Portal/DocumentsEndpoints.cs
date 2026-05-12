using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Commands;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Application.Documents.Queries;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DownloadDocumentFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentFileQuery;
using DeleteDocumentCommand = MaemoCompliance.Application.Documents.Commands.DeleteDocumentCommand;
using GetDocumentVersionsQuery = MaemoCompliance.Application.Documents.Queries.GetDocumentVersionsQuery;
using CreateDocumentVersionCommand = MaemoCompliance.Application.Documents.Commands.CreateDocumentVersionCommand;
using DownloadDocumentVersionFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentVersionFileQuery;
using DocumentVersionDto = MaemoCompliance.Application.Documents.Dtos.DocumentVersionDto;
using PopiaTrailReportItemDto = MaemoCompliance.Application.Documents.Dtos.PopiaTrailReportItemDto;
using GetPopiaTrailReportQuery = MaemoCompliance.Application.Documents.Queries.GetPopiaTrailReportQuery;

namespace MaemoCompliance.Api.Portal;

public static class DocumentsEndpoints
{
    public static void MapDocumentsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // Check module access for all endpoints in this group
        group.AddEndpointFilter(async (context, next) =>
        {
            var moduleChecker = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleChecker>();
            if (!moduleChecker.HasModule("Documents"))
            {
                return ErrorResults.ModuleNotEnabled("Documents");
            }
            return await next(context);
        });

        // GET /api/documents
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] DocumentStatus? status = null,
            [FromQuery] string? department = null,
            [FromQuery] string? category = null,
            [FromQuery] bool includeAllVersions = false) =>
        {
            var query = new GetDocumentsQuery
            {
                Status = status,
                Department = department,
                Category = category,
                IncludeAllVersions = includeAllVersions
            };

            var documents = await mediator.Send(query, cancellationToken);
            return Results.Ok(documents);
        })
        .WithName("GetDocuments")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK);

        // GET /api/documents/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDocumentByIdQuery { Id = id };
            var document = await mediator.Send(query, cancellationToken);

            if (document == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(document);
        })
        .WithName("GetDocumentById")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents
        group.MapPost("/", async (
            CreateDocumentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateDocumentCommand
            {
                Request = request
            };

            var documentId = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/documents/{documentId}", new { id = documentId });
        })
        .WithName("CreateDocument")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/documents/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDocumentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateDocumentCommand
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
        .WithName("UpdateDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // PUT /api/documents/{id}/status
        group.MapPut("/{id:guid}/status", async (
            Guid id,
            ChangeDocumentStatusRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangeDocumentStatusCommand
            {
                DocumentId = id,
                NewStatus = request.Status,
                ApproverUserId = request.ApproverUserId
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
        .WithName("ChangeDocumentStatus")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/submit-for-approval
        group.MapPost("/{id:guid}/submit-for-approval", SubmitDocumentForApprovalImpl)
        .WithName("SubmitDocumentForApproval")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/submit-for-review — alias
        group.MapPost("/{id:guid}/submit-for-review", SubmitDocumentForApprovalImpl)
        .WithName("SubmitDocumentForReview")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/approve
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ApproveDocumentRequest? request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ApproveDocumentCommand
            {
                DocumentId = id,
                Comments = request?.Comments,
                ApproverName = request?.ApproverName,
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
        .RequireAuthorization("RequireDocumentApprover")
        .WithName("ApproveDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/reject
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectDocumentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new RejectDocumentCommand
            {
                DocumentId = id,
                RejectedReason = request.RejectedReason
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
        .WithName("RejectDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/return-for-revision
        group.MapPost("/{id:guid}/return-for-revision", async (
            Guid id,
            ReturnForRevisionRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new RejectDocumentCommand
            {
                DocumentId = id,
                RejectedReason = request.Reason
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
        .WithName("ReturnDocumentForRevision")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/activate
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateDocumentCommand
            {
                DocumentId = id
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
        .WithName("ActivateDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /api/documents/{id}/archive
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await mediator.Send(new ArchiveDocumentCommand { DocumentId = id }, cancellationToken);

                var query = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(query, cancellationToken);
                if (document == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(document);
            }
            catch (KeyNotFoundException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (ConflictException ex)
            {
                return ErrorResults.Conflict("DocumentArchiveConflict", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("ArchiveError", $"Failed to archive document: {ex.Message}");
            }
        })
        .WithName("ArchiveDocument")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/documents/{id}/upload
        group.MapPost("/{id:guid}/upload", async (
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
        .WithName("UploadDocumentFile")
        .WithOpenApi()
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery(); // File upload endpoints typically disable antiforgery

        // GET /api/documents/{id}/download
        group.MapGet("/{id:guid}/download", async (
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
        .WithName("DownloadDocumentFile")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /api/documents/{id}
        group.MapDelete("/{id:guid}", async (
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
        .WithName("DeleteDocument")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/documents/{id}/versions
        group.MapGet("/{id:guid}/versions", async (
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
        .WithName("GetDocumentVersions")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentVersionDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/documents/{id}/versions
        group.MapPost("/{id:guid}/versions", async (
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
                return Results.Created($"/api/documents/{id}/versions/{versionId}", new { id = versionId });
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
        .WithName("CreateDocumentVersion")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();

        // GET /api/documents/{id}/versions/{version}/download
        group.MapGet("/{id:guid}/versions/{version:int}/download", async (
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
        .WithName("DownloadDocumentVersionFile")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/documents/pending-approval
        group.MapGet("/pending-approval", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetPendingApprovalDocumentsQuery();
                var documents = await mediator.Send(query, cancellationToken);
                return Results.Ok(documents);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get pending approval documents: {ex.Message}");
            }
        })
        .WithName("GetPendingApprovalDocuments")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/documents/approvals - Alias for /pending-approval (for frontend compatibility)
        group.MapGet("/approvals", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetPendingApprovalDocumentsQuery();
                var documents = await mediator.Send(query, cancellationToken);
                return Results.Ok(documents);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get pending approval documents: {ex.Message}");
            }
        })
        .WithName("GetApprovals")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/documents/bbbee-certificates/expiring-soon
        group.MapGet("/bbbee-certificates/expiring-soon", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] int days = 90) =>
        {
            try
            {
                var query = new GetBbbeeCertificatesExpiringSoonQuery { Days = days };
                var certificates = await mediator.Send(query, cancellationToken);
                return Results.Ok(certificates);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get BBBEE certificates expiring soon: {ex.Message}");
            }
        })
        .WithName("GetBbbeeCertificatesExpiringSoon")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    public static void MapPopiaTrailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/popiatrail")
            .WithTags("POPIA Compliance");

        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // GET /api/popiatrail/report
        group.MapGet("/report", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] int days = 30) =>
        {
            try
            {
                var query = new GetPopiaTrailReportQuery { Days = days };
                var report = await mediator.Send(query, cancellationToken);
                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("QueryError", $"Failed to get POPIA trail report: {ex.Message}");
            }
        })
        .WithName("GetPopiaTrailReport")
        .WithOpenApi()
        .Produces<IReadOnlyList<PopiaTrailReportItemDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SubmitDocumentForApprovalImpl(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new SubmitDocumentForApprovalCommand
        {
            DocumentId = id
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
    }
}


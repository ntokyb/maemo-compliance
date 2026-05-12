using MaemoCompliance.Portal.Api.Common;
using MaemoCompliance.Application.Documents.Commands;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Application.Documents.Queries;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DownloadDocumentFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentFileQuery;
using DeleteDocumentCommand = MaemoCompliance.Application.Documents.Commands.DeleteDocumentCommand;
using GetDocumentVersionsQuery = MaemoCompliance.Application.Documents.Queries.GetDocumentVersionsQuery;
using CreateDocumentVersionCommand = MaemoCompliance.Application.Documents.Commands.CreateDocumentVersionCommand;
using DownloadDocumentVersionFileQuery = MaemoCompliance.Application.Documents.Queries.DownloadDocumentVersionFileQuery;
using DocumentVersionDto = MaemoCompliance.Application.Documents.Dtos.DocumentVersionDto;
using GetDocumentsPastRetentionQuery = MaemoCompliance.Application.Documents.Queries.GetDocumentsPastRetentionQuery;
using ArchiveDocumentCommand = MaemoCompliance.Application.Documents.Commands.ArchiveDocumentCommand;
using GetDocumentAuditEvidenceQuery = MaemoCompliance.Application.Documents.Queries.GetDocumentAuditEvidenceQuery;
using AuditEvidenceDto = MaemoCompliance.Application.Documents.Dtos.AuditEvidenceDto;

namespace MaemoCompliance.Portal.Api.Portal;

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
            [FromQuery] bool includeAllVersions = false) =>
        {
            var query = new GetDocumentsQuery
            {
                Status = status,
                Department = department,
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
        // DEPRECATED: Use POST /admin/v1/documents/{id}/destroy instead for controlled destruction workflow
        // This endpoint is kept for backward compatibility but should not be used for retention-compliant destruction
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Return deprecation notice - direct deletion is no longer allowed
            return ErrorResults.BadRequest(
                "DeprecatedEndpoint",
                "Direct document deletion is deprecated. Use POST /admin/v1/documents/{id}/destroy for controlled destruction workflow aligned with retention rules.");
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

        // POST /api/documents/{id}/submit-for-approval
        group.MapPost("/{id:guid}/submit-for-approval", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new SubmitDocumentForApprovalCommand { DocumentId = id };
                await mediator.Send(command, cancellationToken);

                // Return updated document
                var query = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(query, cancellationToken);
                return Results.Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidTransition", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("SubmitError", $"Failed to submit document for approval: {ex.Message}");
            }
        })
        .WithName("SubmitDocumentForApproval")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents/{id}/approve
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            [FromBody] ApproveDocumentRequest? request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new ApproveDocumentCommand
                {
                    DocumentId = id,
                    Comments = request?.Comments
                };
                await mediator.Send(command, cancellationToken);

                // Return updated document
                var query = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(query, cancellationToken);
                return Results.Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidTransition", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("ApproveError", $"Failed to approve document: {ex.Message}");
            }
        })
        .WithName("ApproveDocument")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents/{id}/reject
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectDocumentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RejectDocumentCommand
                {
                    DocumentId = id,
                    RejectedReason = request.RejectedReason
                };
                await mediator.Send(command, cancellationToken);

                // Return updated document
                var query = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(query, cancellationToken);
                return Results.Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidTransition", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("RejectError", $"Failed to reject document: {ex.Message}");
            }
        })
        .WithName("RejectDocument")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents/{id}/activate
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new ActivateDocumentCommand { DocumentId = id };
                await mediator.Send(command, cancellationToken);

                // Return updated document
                var query = new GetDocumentByIdQuery { Id = id };
                var document = await mediator.Send(query, cancellationToken);
                return Results.Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidTransition", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("ActivateError", $"Failed to activate document: {ex.Message}");
            }
        })
        .WithName("ActivateDocument")
        .WithOpenApi()
        .Produces<DocumentDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/documents/{id}/archive
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new ArchiveDocumentCommand { DocumentId = id };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
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
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/documents/past-retention (Admin endpoint for records retention)
        group.MapGet("/past-retention", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDocumentsPastRetentionQuery();
            var documents = await mediator.Send(query, cancellationToken);
            return Results.Ok(documents);
        })
        .WithName("GetDocumentsPastRetention")
        .WithOpenApi()
        .Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK);

        // GET /api/documents/{id}/audit-evidence
        group.MapGet("/{id}/audit-evidence", async (
            Guid id,
            IMediator mediator,
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            ICurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var tenantId = tenantProvider.GetCurrentTenantId();
                var currentUserId = currentUserService.UserId;

                // Permission check: Verify user has access to generate evidence
                // Allowed: Document owner, Tenant admin (via AdminEmail), or Consultant assigned to tenant
                var document = await context.Documents
                    .Where(d => d.Id == id && d.TenantId == tenantId)
                    .Select(d => new { d.OwnerUserId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (document == null)
                {
                    return ErrorResults.NotFound("DocumentNotFound", $"Document with ID {id} not found for current tenant.");
                }

                // Check permissions
                bool hasPermission = false;

                // 1. Check if user is document owner
                if (!string.IsNullOrEmpty(currentUserId) && 
                    !string.IsNullOrEmpty(document.OwnerUserId) && 
                    currentUserId.Equals(document.OwnerUserId, StringComparison.OrdinalIgnoreCase))
                {
                    hasPermission = true;
                }

                // 2. Check if user is a consultant assigned to this tenant
                if (!hasPermission && !string.IsNullOrEmpty(currentUserId))
                {
                    var isConsultant = await context.ConsultantTenantLinks
                        .AnyAsync(link => 
                            link.TenantId == tenantId && 
                            link.ConsultantUserId.ToString() == currentUserId &&
                            link.IsActive,
                            cancellationToken);
                    
                    if (isConsultant)
                    {
                        hasPermission = true;
                    }
                }

                // 3. Check if user is tenant admin (via AdminEmail - simplified check)
                // Note: In production, this should check against Tenant.AdminEmail or a proper admin role
                // For now, we'll allow if user has access to the tenant context
                // TODO: Implement proper tenant admin role check

                if (!hasPermission)
                {
                    return ErrorResults.Forbidden(
                        "InsufficientPermissions",
                        "You do not have permission to generate audit evidence for this document.",
                        "Only document owners, tenant administrators, or assigned consultants can generate audit evidence.");
                }

                // Execute query
                var query = new GetDocumentAuditEvidenceQuery { DocumentId = id };
                var evidence = await mediator.Send(query, cancellationToken);
                return Results.Ok(evidence);
            }
            catch (KeyNotFoundException ex)
            {
                return ErrorResults.NotFound("DocumentNotFound", ex.Message);
            }
            catch (Exception)
            {
                // Log the exception but don't expose internal details to client
                return ErrorResults.BadRequest(
                    "AuditEvidenceError",
                    "Failed to retrieve audit evidence.",
                    "An error occurred while generating the audit evidence report. Please try again later.");
            }
        })
        .WithName("GetDocumentAuditEvidence")
        .WithOpenApi()
        .Produces<AuditEvidenceDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}


using FluentAssertions;
using MaemoCompliance.Application.Documents.Commands;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.UnitTests.Support;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.UnitTests.Documents;

public sealed class DocumentVersionNumberTests
{
    [Fact]
    public async Task First_version_of_document_is_1_0()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var docId = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = docId,
            TenantId = ctx.TenantId,
            Title = "New",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.PendingApproval,
            Status = DocumentStatus.UnderReview,
            StorageLocation = "s3://bucket/f.pdf",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var approve = new ApproveDocumentCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);

        await approve.Handle(new ApproveDocumentCommand { DocumentId = docId }, default);

        var d = await ctx.Db.Documents.AsNoTracking().FirstAsync(x => x.Id == docId);
        DocumentSemanticVersion.Format(d.Version).Should().Be("1.0");
    }

    [Fact]
    public async Task Second_version_increments_to_2_0()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var v1 = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = v1,
            TenantId = ctx.TenantId,
            Title = "Doc",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.Approved,
            Status = DocumentStatus.Approved,
            StorageLocation = "s3://bucket/v1.pdf",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var newVer = new CreateNewDocumentVersionCommandHandler(ctx.Db, ctx.TenantProvider, ctx.Clock, ctx.CurrentUser);
        var v2Id = await newVer.Handle(
            new CreateNewDocumentVersionCommand
            {
                ExistingDocumentId = v1,
                Request = new CreateNewDocumentVersionRequest
                {
                    Title = "Doc",
                    ReviewDate = ctx.Clock.UtcNow,
                },
            },
            default);

        var v2 = await ctx.Db.Documents.AsNoTracking().FirstAsync(d => d.Id == v2Id);
        DocumentSemanticVersion.Format(v2.Version).Should().Be("2.0");
    }
}

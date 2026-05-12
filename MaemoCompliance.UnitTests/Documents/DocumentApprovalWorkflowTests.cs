using FluentAssertions;
using MaemoCompliance.Application.Documents.Commands;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.UnitTests.Support;

namespace MaemoCompliance.UnitTests.Documents;

public sealed class DocumentApprovalWorkflowTests
{
    [Fact]
    public async Task Document_in_Draft_can_be_submitted_for_review()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var docId = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = docId,
            TenantId = ctx.TenantId,
            Title = "Q1 Policy",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.Draft,
            Status = DocumentStatus.Draft,
            StorageLocation = "s3://bucket/file.pdf",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var submit = new SubmitDocumentForApprovalCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);

        await submit.Handle(new SubmitDocumentForApprovalCommand { DocumentId = docId }, default);

        var updated = await ctx.Db.Documents.AsNoTracking().FirstAsync(d => d.Id == docId);
        updated.WorkflowState.Should().Be(DocumentWorkflowState.PendingApproval);
        updated.Status.Should().Be(DocumentStatus.UnderReview);
        updated.SubmittedForReviewAt.Should().Be(ctx.Clock.UtcNow);
    }

    [Fact]
    public async Task Document_in_UnderReview_can_be_approved()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var docId = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = docId,
            TenantId = ctx.TenantId,
            Title = "Procedure",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.PendingApproval,
            Status = DocumentStatus.UnderReview,
            StorageLocation = "s3://bucket/p.pdf",
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

        await approve.Handle(
            new ApproveDocumentCommand { DocumentId = docId, ApproverName = "Pat Approver" },
            default);

        var updated = await ctx.Db.Documents.AsNoTracking().FirstAsync(d => d.Id == docId);
        updated.WorkflowState.Should().Be(DocumentWorkflowState.Approved);
        updated.Status.Should().Be(DocumentStatus.Approved);
        updated.ApprovedBy.Should().Be("Pat Approver");
        updated.ApprovedAt.Should().Be(ctx.Clock.UtcNow);
    }

    [Fact]
    public async Task Document_in_UnderReview_can_be_returned_for_revision()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var docId = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = docId,
            TenantId = ctx.TenantId,
            Title = "Doc",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.PendingApproval,
            Status = DocumentStatus.UnderReview,
            StorageLocation = "s3://bucket/x.pdf",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var reject = new RejectDocumentCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);

        await reject.Handle(
            new RejectDocumentCommand { DocumentId = docId, RejectedReason = "Missing signature" },
            default);

        var updated = await ctx.Db.Documents.AsNoTracking().FirstAsync(d => d.Id == docId);
        updated.WorkflowState.Should().Be(DocumentWorkflowState.Draft);
        updated.Status.Should().Be(DocumentStatus.Draft);
        updated.RejectedReason.Should().Be("Missing signature");
    }

    [Fact]
    public async Task Approved_document_becomes_Obsolete_when_new_version_approved()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var v1 = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = v1,
            TenantId = ctx.TenantId,
            Title = "Policy",
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
        var v2 = await newVer.Handle(
            new CreateNewDocumentVersionCommand
            {
                ExistingDocumentId = v1,
                Request = new CreateNewDocumentVersionRequest
                {
                    Title = "Policy",
                    ReviewDate = ctx.Clock.UtcNow,
                },
            },
            default);

        var v2Doc = await ctx.Db.Documents.FirstAsync(d => d.Id == v2);
        v2Doc.StorageLocation = "s3://bucket/v2.pdf";
        await ctx.Db.SaveChangesAsync();

        var submit = new SubmitDocumentForApprovalCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);
        await submit.Handle(new SubmitDocumentForApprovalCommand { DocumentId = v2 }, default);

        var approve = new ApproveDocumentCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);
        await approve.Handle(new ApproveDocumentCommand { DocumentId = v2, ApproverName = "QA" }, default);

        var v1Reload = await ctx.Db.Documents.AsNoTracking().FirstAsync(d => d.Id == v1);
        v1Reload.WorkflowState.Should().Be(DocumentWorkflowState.Obsolete);
        v1Reload.Status.Should().Be(DocumentStatus.Obsolete);
        v1Reload.SupersededByDocumentId.Should().Be(v2);
    }

    [Fact]
    public async Task Document_cannot_transition_from_Approved_directly_to_UnderReview()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var docId = Guid.NewGuid();
        ctx.Db.Documents.Add(new Document
        {
            Id = docId,
            TenantId = ctx.TenantId,
            Title = "Locked",
            ReviewDate = ctx.Clock.UtcNow,
            WorkflowState = DocumentWorkflowState.Approved,
            Status = DocumentStatus.Approved,
            StorageLocation = "s3://bucket/a.pdf",
            IsCurrentVersion = true,
            Version = 1,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var submit = new SubmitDocumentForApprovalCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object);

        var act = async () => await submit.Handle(new SubmitDocumentForApprovalCommand { DocumentId = docId }, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

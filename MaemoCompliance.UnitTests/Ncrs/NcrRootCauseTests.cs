using FluentAssertions;
using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Application.Ncrs.Commands;
using MaemoCompliance.Application.Ncrs.Queries;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.UnitTests.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MaemoCompliance.UnitTests.Ncrs;

public sealed class NcrRootCauseTests
{
    [Fact]
    public async Task NCR_root_cause_fields_can_be_set()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var ncrId = Guid.NewGuid();
        ctx.Db.Ncrs.Add(new Ncr
        {
            Id = ncrId,
            TenantId = ctx.TenantId,
            Title = "Issue",
            Description = "desc",
            Severity = NcrSeverity.Medium,
            Status = NcrStatus.Open,
            CreatedAt = ctx.Clock.UtcNow,
        });
        await ctx.Db.SaveChangesAsync();

        var getById = new GetNcrByIdQueryHandler(ctx.Db, ctx.TenantProvider);
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetNcrByIdQuery>(), It.IsAny<CancellationToken>()))
            .Returns<GetNcrByIdQuery, CancellationToken>((q, ct) => getById.Handle(q, ct));

        var handler = new UpdateNcrRootCauseCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            mediator.Object);

        var dto = await handler.Handle(
            new UpdateNcrRootCauseCommand
            {
                NcrId = ncrId,
                RootCauseMethod = "5-Why",
                RootCause = "Training gap",
                CorrectiveActionPlan = "Retrain team",
                CorrectiveActionOwner = "John",
                CorrectiveActionDueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            default);

        dto.Should().NotBeNull();
        dto!.RootCauseMethod.Should().Be("5-Why");
        dto.RootCause.Should().Be("Training gap");
        dto.CorrectiveActionPlan.Should().Be("Retrain team");
        dto.CorrectiveActionOwner.Should().Be("John");
        dto.CorrectiveActionDueDate.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var reloaded = await ctx.Db.Ncrs.AsNoTracking().FirstAsync(n => n.Id == ncrId);
        reloaded.RootCause.Should().Be("Training gap");
    }

    [Fact]
    public async Task NCR_effectiveness_can_be_confirmed()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var ncrId = Guid.NewGuid();
        ctx.Db.Ncrs.Add(new Ncr
        {
            Id = ncrId,
            TenantId = ctx.TenantId,
            Title = "Issue",
            Description = "desc",
            Severity = NcrSeverity.Medium,
            Status = NcrStatus.Open,
            CreatedAt = ctx.Clock.UtcNow,
            CorrectiveActionCompletedAt = ctx.Clock.UtcNow.AddDays(-1),
        });
        await ctx.Db.SaveChangesAsync();

        var handler = new ConfirmNcrEffectivenessCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser);

        await handler.Handle(new ConfirmNcrEffectivenessCommand { NcrId = ncrId }, default);

        var reloaded = await ctx.Db.Ncrs.AsNoTracking().FirstAsync(n => n.Id == ncrId);
        reloaded.EffectivenessConfirmed.Should().BeTrue();
        reloaded.EffectivenessVerifiedAt.Should().Be(ctx.Clock.UtcNow);
    }

    [Fact]
    public async Task NCR_linked_to_audit_finding_stores_reference()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var findingId = Guid.NewGuid();

        var createdId = await new CreateNcrCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser,
            ctx.AuditLogger.Object,
            ctx.BusinessAuditLogger.Object).Handle(
            new CreateNcrCommand
            {
                Title = "From finding",
                Description = "d",
                Severity = NcrSeverity.Low,
                LinkedAuditFindingId = findingId,
            },
            default);

        var ncr = await ctx.Db.Ncrs.AsNoTracking().FirstAsync(n => n.Id == createdId);
        ncr.LinkedAuditFindingId.Should().Be(findingId);
    }
}

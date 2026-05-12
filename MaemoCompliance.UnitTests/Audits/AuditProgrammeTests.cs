using FluentAssertions;
using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Users;
using MaemoCompliance.UnitTests.Support;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.UnitTests.Audits;

public sealed class AuditProgrammeTests
{
    [Fact]
    public async Task AuditProgramme_can_be_created_for_year()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var handler = new CreateAuditProgrammeCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser);

        var id = await handler.Handle(
            new CreateAuditProgrammeCommand
            {
                Year = 2026,
                Title = "Annual programme",
                Items =
                [
                    new CreateAuditProgrammeItem
                    {
                        ProcessArea = "Document Control",
                        AuditorName = "Jane",
                        PlannedDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                    },
                ],
            },
            default);

        var programme = await ctx.Db.AuditProgrammes.Include(p => p.Items).AsNoTracking().FirstAsync(p => p.Id == id);
        programme.Year.Should().Be(2026);
        programme.Title.Should().Be("Annual programme");
        programme.Status.Should().Be(AuditProgrammeStatus.Draft);
        programme.Items.Should().HaveCount(1);
        programme.Items.First().ProcessArea.Should().Be("Document Control");
    }

    [Fact]
    public async Task AuditScheduleItem_links_to_completed_audit()
    {
        using var ctx = new ComplianceHandlersTestContext();
        var programmeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var auditId = Guid.NewGuid();

        ctx.Db.AuditProgrammes.Add(new AuditProgramme
        {
            Id = programmeId,
            TenantId = ctx.TenantId,
            Year = 2026,
            Title = "P",
            Status = AuditProgrammeStatus.Draft,
            CreatedAt = ctx.Clock.UtcNow,
            Items =
            [
                new AuditScheduleItem
                {
                    Id = itemId,
                    TenantId = ctx.TenantId,
                    AuditProgrammeId = programmeId,
                    ProcessArea = "QA",
                    AuditorName = "Jane",
                    PlannedDate = ctx.Clock.UtcNow.AddDays(7),
                    Status = AuditScheduleItemStatus.Planned,
                    CreatedAt = ctx.Clock.UtcNow,
                },
            ],
        });

        var consultantId = Guid.NewGuid();
        ctx.Db.Users.Add(new User
        {
            Id = consultantId,
            TenantId = ctx.TenantId,
            Email = "consultant-unit@test.com",
            FullName = "Consultant",
            Role = UserRole.Consultant,
            IsActive = true,
            CreatedAt = ctx.Clock.UtcNow,
        });
        var templateId = Guid.NewGuid();
        ctx.Db.AuditTemplates.Add(new AuditTemplate
        {
            Id = templateId,
            ConsultantUserId = consultantId,
            Name = "Unit template",
            CreatedAt = ctx.Clock.UtcNow,
        });

        ctx.Db.AuditRuns.Add(new AuditRun
        {
            Id = auditId,
            TenantId = ctx.TenantId,
            AuditTemplateId = templateId,
            StartedAt = ctx.Clock.UtcNow,
            CreatedAt = ctx.Clock.UtcNow,
        });

        await ctx.Db.SaveChangesAsync();

        var linkHandler = new LinkAuditToScheduleItemCommandHandler(
            ctx.Db,
            ctx.TenantProvider,
            ctx.Clock,
            ctx.CurrentUser);

        await linkHandler.Handle(
            new LinkAuditToScheduleItemCommand
            {
                ProgrammeId = programmeId,
                ItemId = itemId,
                AuditId = auditId,
            },
            default);

        var item = await ctx.Db.AuditScheduleItems.AsNoTracking().FirstAsync(i => i.Id == itemId);
        item.LinkedAuditId.Should().Be(auditId);
        item.Status.Should().Be(AuditScheduleItemStatus.Complete);
    }

    [Fact]
    public void AuditScheduleItem_is_Overdue_when_PlannedDate_past_and_no_audit()
    {
        var item = new AuditScheduleItem
        {
            Status = AuditScheduleItemStatus.Planned,
            PlannedDate = DateTime.UtcNow.AddDays(-1),
        };

        item.ApplyOverdueIfNeeded(DateTime.UtcNow);

        item.Status.Should().Be(AuditScheduleItemStatus.Overdue);
    }
}

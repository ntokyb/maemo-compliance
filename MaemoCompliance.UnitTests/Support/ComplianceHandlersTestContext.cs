using MaemoCompliance.Application.Common;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MaemoCompliance.UnitTests.Support;

public sealed class ComplianceHandlersTestContext : IDisposable
{
    public Guid TenantId { get; } = Guid.NewGuid();
    public MaemoComplianceDbContext Db { get; }
    public FixedTenantProvider TenantProvider { get; }
    public FixedClock Clock { get; }
    public FixedCurrentUser CurrentUser { get; }
    public Mock<IAuditLogger> AuditLogger { get; } = new();
    public Mock<IBusinessAuditLogger> BusinessAuditLogger { get; } = new();

    public ComplianceHandlersTestContext()
    {
        TenantProvider = new FixedTenantProvider(TenantId);
        Clock = new FixedClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        CurrentUser = new FixedCurrentUser("unit-test-user", "unit@test.com");

        AuditLogger
            .Setup(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        BusinessAuditLogger
            .Setup(b => b.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        BusinessAuditLogger
            .Setup(b => b.LogForTenantAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<MaemoComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Db = new MaemoComplianceDbContext(options, TenantProvider);
    }

    public void Dispose() => Db.Dispose();
}

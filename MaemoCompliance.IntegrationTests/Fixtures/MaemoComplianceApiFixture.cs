using System.Text.Json;
using MaemoCompliance.Api;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Tenants;
using MaemoCompliance.Domain.Users;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace MaemoCompliance.IntegrationTests.Fixtures;

public class MaemoComplianceApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private PostgreSqlContainer? _postgres;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var removeTypes = new[]
            {
                typeof(DbContextOptions<MaemoComplianceDbContext>),
                typeof(MaemoComplianceDbContext),
            };
            foreach (var t in removeTypes)
            {
                foreach (var d in services.Where(x => x.ServiceType == t).ToList())
                {
                    services.Remove(d);
                }
            }

            services.AddDbContext<MaemoComplianceDbContext>(options =>
                options.UseNpgsql(_postgres!.GetConnectionString()));
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");

        var postgres = new PostgreSqlBuilder()
            .WithDatabase("maemo_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        try
        {
            await postgres.StartAsync();
        }
        catch
        {
            await postgres.DisposeAsync();
            throw;
        }

        _postgres = postgres;

        using (CreateClient())
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
            db.Database.Migrate();
            EnsureTestTenant(db);
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    private Guid? _integrationAuditTemplateId;

    /// <summary>Seeds a consultant user and audit template for integration tests that need audit runs.</summary>
    public async Task<Guid> EnsureIntegrationAuditTemplateIdAsync()
    {
        if (_integrationAuditTemplateId.HasValue)
        {
            return _integrationAuditTemplateId.Value;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var consultantId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = consultantId,
            TenantId = TestTenantId,
            Email = $"consultant-{consultantId:N}@example.com",
            FullName = "Integration Consultant",
            Role = UserRole.Consultant,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tests",
        });

        var templateId = Guid.NewGuid();
        db.AuditTemplates.Add(new AuditTemplate
        {
            Id = templateId,
            ConsultantUserId = consultantId,
            Name = "Integration Audit Template",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tests",
        });

        await db.SaveChangesAsync();
        _integrationAuditTemplateId = templateId;
        return templateId;
    }

    public async Task SetDocumentStorageLocationAsync(Guid documentId, string storageLocation)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == TestTenantId);
        if (doc == null)
        {
            throw new InvalidOperationException($"Document {documentId} not found for test tenant.");
        }

        doc.StorageLocation = storageLocation;
        await db.SaveChangesAsync();
    }

    public async Task ResetTenantAuditDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var tid = TestTenantId;

        var items = db.AuditScheduleItems.Where(i => i.TenantId == tid);
        var programmes = db.AuditProgrammes.Where(p => p.TenantId == tid);
        db.AuditScheduleItems.RemoveRange(items);
        db.AuditProgrammes.RemoveRange(programmes);

        var findings = db.AuditFindings.Where(f => f.TenantId == tid);
        db.AuditFindings.RemoveRange(findings);

        var answers = db.AuditAnswers.Where(a => a.TenantId == tid);
        db.AuditAnswers.RemoveRange(answers);

        var runs = db.AuditRuns.Where(r => r.TenantId == tid);
        db.AuditRuns.RemoveRange(runs);

        await db.SaveChangesAsync();
    }

    public async Task<Ncr?> GetNcrEntityAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        return await db.Ncrs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == TestTenantId);
    }

    /// <summary>Clears document rows for the shared integration tenant so each test starts from a predictable state.</summary>
    public async Task ResetTenantDocumentsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var tid = TestTenantId;
        var docIds = db.Documents.Where(d => d.TenantId == tid).Select(d => d.Id).ToList();
        var versions = db.DocumentVersions.Where(v => docIds.Contains(v.DocumentId));
        db.DocumentVersions.RemoveRange(versions);
        var docs = db.Documents.Where(d => d.TenantId == tid);
        db.Documents.RemoveRange(docs);
        await db.SaveChangesAsync();
    }

    /// <summary>Clears NCR-related rows for the shared integration tenant.</summary>
    public async Task ResetTenantNcrsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var tid = TestTenantId;
        var links = db.NcrRiskLinks.Where(l => l.TenantId == tid);
        var history = db.NcrStatusHistory.Where(h => h.TenantId == tid);
        var ncrs = db.Ncrs.Where(n => n.TenantId == tid);
        db.NcrRiskLinks.RemoveRange(links);
        db.NcrStatusHistory.RemoveRange(history);
        db.Ncrs.RemoveRange(ncrs);
        await db.SaveChangesAsync();
    }

    private static void EnsureTestTenant(MaemoComplianceDbContext db)
    {
        if (db.Tenants.Any(t => t.Id == TestTenantId))
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = TestTenantId,
            Name = "Integration Test Tenant",
            AdminEmail = "integration-test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ModulesEnabledJson = JsonSerializer.Serialize(new[] { "Documents", "NCR", "Risks", "Audits" }),
        };
        db.Tenants.Add(tenant);
        db.SaveChanges();
    }
}

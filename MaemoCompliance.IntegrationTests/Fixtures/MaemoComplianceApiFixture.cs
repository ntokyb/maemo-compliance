using System.Text.Json;
using MaemoCompliance.Api;
using MaemoCompliance.Domain.Tenants;
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

    /// <summary>Clears document rows for the shared integration tenant so each test starts from a predictable state.</summary>
    public async Task ResetTenantDocumentsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
        var docs = db.Documents.Where(d => d.TenantId == TestTenantId);
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

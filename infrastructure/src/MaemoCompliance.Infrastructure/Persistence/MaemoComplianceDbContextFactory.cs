using MaemoCompliance.Application.Common;
using MaemoCompliance.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Infrastructure.Persistence;

public class MaemoComplianceDbContextFactory : IDesignTimeDbContextFactory<MaemoComplianceDbContext>
{
    public MaemoComplianceDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../MaemoCompliance.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Build options
        var optionsBuilder = new DbContextOptionsBuilder<MaemoComplianceDbContext>();
        var connectionString = configuration.GetConnectionString("MaemoDatabase");
        optionsBuilder.UseNpgsql(connectionString);

        // Create a stub tenant provider for design-time
        var tenantContext = new TenantContext();
        var tenantProvider = new TenantProvider(tenantContext);

        return new MaemoComplianceDbContext(optionsBuilder.Options, tenantProvider);
    }
}


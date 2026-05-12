using Maemo.Application.Common;
using Maemo.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Maemo.Infrastructure.Persistence;

public class MaemoDbContextFactory : IDesignTimeDbContextFactory<MaemoDbContext>
{
    public MaemoDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Maemo.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Build options
        var optionsBuilder = new DbContextOptionsBuilder<MaemoDbContext>();
        var connectionString = configuration.GetConnectionString("MaemoDatabase");
        optionsBuilder.UseNpgsql(connectionString);

        // Create a stub tenant provider for design-time
        var tenantContext = new TenantContext();
        var tenantProvider = new TenantProvider(tenantContext);

        return new MaemoDbContext(optionsBuilder.Options, tenantProvider);
    }
}


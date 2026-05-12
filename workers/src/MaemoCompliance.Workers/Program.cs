using MaemoCompliance.Application;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Infrastructure;
using MaemoCompliance.Infrastructure.Logging;
using MaemoCompliance.Workers.Services;
using Serilog;

// Configure initial logger (will be replaced after full configuration)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Maemo Workers");

    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, configuration) =>
        {
            SerilogConfiguration.ConfigureSerilog(configuration, context.Configuration, services);
        })
        .ConfigureServices((context, services) =>
        {
                   // Add Application services (MediatR, validators, etc.)
                   services.AddApplication();

                   // Add Infrastructure services (DbContext, TenantProvider, DateTimeProvider, etc.)
                   services.AddInfrastructure(context.Configuration);

                   // Register the heartbeat worker
                   services.AddHostedService<HeartbeatWorker>();

                   // Register the compliance jobs worker
                   services.AddHostedService<ComplianceJobsWorker>();

                   // Register the document destruction worker (optional - controlled by config)
                   services.AddHostedService<DocumentDestructionWorker>();

                   // Register the records retention worker (daily check for documents past retention)
                   services.AddHostedService<RecordsRetentionWorker>();
        });

    var host = builder.Build();
    
    // Log deployment mode
    var deploymentContext = host.Services.GetRequiredService<IDeploymentContext>();
    Log.Information("Maemo Workers running in {Mode} mode", deploymentContext.Mode);
    
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

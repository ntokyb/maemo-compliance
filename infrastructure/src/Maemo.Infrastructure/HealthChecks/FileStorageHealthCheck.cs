using Maemo.Application.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Maemo.Infrastructure.HealthChecks;

public class FileStorageHealthCheck : IHealthCheck
{
    private readonly IFileStorageProvider? _fileStorageProvider;
    private readonly IDeploymentContext _deploymentContext;
    private readonly ILogger<FileStorageHealthCheck> _logger;

    public FileStorageHealthCheck(
        IFileStorageProvider? fileStorageProvider,
        IDeploymentContext deploymentContext,
        ILogger<FileStorageHealthCheck> logger)
    {
        _fileStorageProvider = fileStorageProvider;
        _deploymentContext = deploymentContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Skip file storage check if provider is not available (e.g., SaaS mode without M365)
        if (_fileStorageProvider == null)
        {
            return HealthCheckResult.Healthy("File storage check skipped (not configured)");
        }

        try
        {
            // Create a test file to verify write access
            // Use a dedicated health check tenant ID (not Guid.Empty to avoid validation issues)
            var testTenantId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
            var testContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("health-check-test"));
            var testFileName = $"health-check-{Guid.NewGuid()}.tmp";
            var testCategory = "health-checks";

            // Try to save a test file
            var storagePath = await _fileStorageProvider.SaveAsync(
                testTenantId,
                testContent,
                testFileName,
                testCategory,
                cancellationToken);

            // Try to read it back
            var retrievedStream = await _fileStorageProvider.GetAsync(
                testTenantId,
                storagePath,
                cancellationToken);

            if (retrievedStream == null)
            {
                _logger.LogWarning("File storage health check failed: Could not retrieve test file");
                return HealthCheckResult.Unhealthy("File storage read test failed");
            }

            // Clean up test file
            try
            {
                await _fileStorageProvider.DeleteAsync(testTenantId, storagePath, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                // Log but don't fail health check if cleanup fails
                _logger.LogWarning(deleteEx, "Failed to clean up health check test file: {StoragePath}", storagePath);
            }

            return HealthCheckResult.Healthy("File storage is available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File storage health check failed with exception");
            return HealthCheckResult.Unhealthy("File storage check failed", ex);
        }
    }
}


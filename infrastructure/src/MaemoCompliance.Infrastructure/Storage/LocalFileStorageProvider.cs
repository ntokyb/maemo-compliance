using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Infrastructure.Storage;

public class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    public LocalFileStorageProvider(
        IConfiguration configuration,
        ILogger<LocalFileStorageProvider> logger)
    {
        _basePath = configuration["Storage:LocalBasePath"] 
            ?? throw new InvalidOperationException("Storage:LocalBasePath configuration is required for LocalFileStorageProvider");
        _logger = logger;

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage base directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> SaveAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string category,
        CancellationToken cancellationToken = default)
    {
        // Sanitize category and fileName
        var sanitizedCategory = SanitizePathSegment(category ?? "General");
        var sanitizedFileName = SanitizeFileName(fileName);
        
        // Generate unique file name using GUID to avoid collisions
        var fileExtension = Path.GetExtension(sanitizedFileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        
        // Build full path: {basePath}/{tenantId}/{category}/{uniqueFileName}
        var tenantFolder = Path.Combine(_basePath, tenantId.ToString());
        var categoryFolder = Path.Combine(tenantFolder, sanitizedCategory);
        var fullPath = Path.Combine(categoryFolder, uniqueFileName);

        // Ensure directories exist
        Directory.CreateDirectory(categoryFolder);

        // Save file
        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        // Return storage path relative to base path: "{tenantId}/{category}/{uniqueFileName}"
        var storagePath = $"{tenantId}/{sanitizedCategory}/{uniqueFileName}";

        _logger.LogInformation(
            "File saved to local storage: {StoragePath}",
            storagePath);

        return storagePath;
    }

    public Task<Stream?> GetAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        // Build full path from storage path
        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {StoragePath}", storagePath);
            return Task.FromResult<Stream?>(null);
        }

        // Verify the file belongs to the tenant (security check)
        var expectedTenantId = storagePath.Split(Path.DirectorySeparatorChar)[0];
        if (expectedTenantId != tenantId.ToString())
        {
            _logger.LogWarning(
                "Tenant mismatch: requested tenant {RequestedTenantId}, but storage path belongs to {PathTenantId}",
                tenantId,
                expectedTenantId);
            return Task.FromResult<Stream?>(null);
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task DeleteAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        // Build full path from storage path
        var fullPath = Path.Combine(_basePath, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found for deletion: {StoragePath}", storagePath);
            return Task.CompletedTask;
        }

        // Verify the file belongs to the tenant (security check)
        var expectedTenantId = storagePath.Split(Path.DirectorySeparatorChar)[0];
        if (expectedTenantId != tenantId.ToString())
        {
            _logger.LogWarning(
                "Tenant mismatch during deletion: requested tenant {RequestedTenantId}, but storage path belongs to {PathTenantId}",
                tenantId,
                expectedTenantId);
            return Task.CompletedTask;
        }

        File.Delete(fullPath);
        _logger.LogInformation("File deleted: {StoragePath}", storagePath);

        return Task.CompletedTask;
    }

    private static string SanitizePathSegment(string segment)
    {
        // Remove invalid path characters
        var invalidChars = Path.GetInvalidPathChars();
        var sanitized = new string(segment
            .Where(c => !invalidChars.Contains(c) && c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
            .ToArray());
        
        return string.IsNullOrWhiteSpace(sanitized) ? "General" : sanitized;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());
        
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}


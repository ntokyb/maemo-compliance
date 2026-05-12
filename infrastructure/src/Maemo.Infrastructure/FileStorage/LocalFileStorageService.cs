using Maemo.Application.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maemo.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var localFolderPath = Path.Combine(_environment.ContentRootPath ?? AppDomain.CurrentDomain.BaseDirectory, "local-files", tenantId.ToString());
        
        // Ensure directory exists
        Directory.CreateDirectory(localFolderPath);

        var localFilePath = Path.Combine(localFolderPath, fileName);
        
        // Save file to local storage
        using (var fileStream = new FileStream(localFilePath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var storageLocation = $"local-files/{tenantId}/{fileName}";
        
        _logger.LogInformation(
            "File saved to local storage: {StorageLocation}",
            storageLocation);

        return storageLocation;
    }
}


namespace Maemo.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default);
}


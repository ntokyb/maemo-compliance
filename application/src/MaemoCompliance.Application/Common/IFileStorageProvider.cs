namespace MaemoCompliance.Application.Common;

public interface IFileStorageProvider
{
    Task<string> SaveAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string category,
        CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default);
}


namespace Maemo.Application.Common;

/// <summary>
/// Service for computing file hashes for integrity verification.
/// </summary>
public interface IFileHashService
{
    /// <summary>
    /// Computes SHA256 hash of the provided stream and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SHA256 hash as hexadecimal string (64 characters).</returns>
    Task<string> ComputeSha256HashAsync(Stream stream, CancellationToken cancellationToken = default);
}


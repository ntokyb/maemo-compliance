using Maemo.Application.Common;
using System.Security.Cryptography;

namespace Maemo.Infrastructure.FileStorage;

/// <summary>
/// Service for computing file hashes using SHA256.
/// </summary>
public class FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256HashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        
        // Reset stream position to beginning if needed
        if (stream.CanSeek && stream.Position > 0)
        {
            stream.Position = 0;
        }

        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}


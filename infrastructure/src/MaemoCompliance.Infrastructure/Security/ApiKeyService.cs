using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Security;
using MaemoCompliance.Domain.Security;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace MaemoCompliance.Infrastructure.Security;

/// <summary>
/// Implementation of API key service for managing engine API keys.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly MaemoComplianceDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApiKeyService(MaemoComplianceDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiKey?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var key = await _context.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive, cancellationToken);

        return key;
    }

    public async Task<ApiKey> CreateAsync(Guid tenantId, string? name, CancellationToken cancellationToken = default)
    {
        // Generate a secure random API key
        // Format: maemo_{32-byte-base64} (e.g., maemo_AbCdEf123456...)
        var keyBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        var keyValue = "maemo_" + Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", ""); // URL-safe base64

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = keyValue,
            Name = name,
            IsActive = true,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return apiKey;
    }

    public async Task RevokeAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey != null)
        {
            apiKey.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}


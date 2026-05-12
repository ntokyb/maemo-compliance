using System.Net.Http.Json;
using Maemo.Engine.Client.Models;

namespace Maemo.Engine.Client;

/// <summary>
/// Implementation of the Audit client for the Maemo Compliance Engine API.
/// </summary>
public class AuditClient : IAuditClient
{
    private readonly HttpClient _httpClient;

    internal AuditClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/engine/v1/audits/templates", cancellationToken);
        response.EnsureSuccessStatusCode();

        var templates = await response.Content.ReadFromJsonAsync<List<AuditTemplateDto>>(cancellationToken: cancellationToken);
        return templates ?? new List<AuditTemplateDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditRunDto>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/engine/v1/audits/runs", cancellationToken);
        response.EnsureSuccessStatusCode();

        var runs = await response.Content.ReadFromJsonAsync<List<AuditRunDto>>(cancellationToken: cancellationToken);
        return runs ?? new List<AuditRunDto>();
    }

    /// <inheritdoc />
    public async Task<Guid> StartRunAsync(Guid auditTemplateId, string? auditorUserId = null, CancellationToken cancellationToken = default)
    {
        var request = new StartAuditRunRequest
        {
            AuditTemplateId = auditTemplateId,
            AuditorUserId = auditorUserId
        };

        var response = await _httpClient.PostAsJsonAsync("/engine/v1/audits/runs", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StartAuditRunResponse>(cancellationToken: cancellationToken);
        return result?.Id ?? Guid.Empty;
    }
}

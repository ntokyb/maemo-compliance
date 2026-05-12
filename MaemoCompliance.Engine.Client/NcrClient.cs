using System.Net.Http.Json;
using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Implementation of the NCR client for the Maemo Compliance Engine API.
/// </summary>
public class NcrClient : INcrClient
{
    private readonly HttpClient _httpClient;

    internal NcrClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NcrDto>> GetNcrsAsync(
        NcrStatus? status = null,
        NcrSeverity? severity = null,
        string? department = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");
        if (severity.HasValue)
            queryParams.Add($"severity={severity.Value}");
        if (!string.IsNullOrWhiteSpace(department))
            queryParams.Add($"department={Uri.EscapeDataString(department)}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/engine/v1/ncr{queryString}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var ncrs = await response.Content.ReadFromJsonAsync<List<NcrDto>>(cancellationToken: cancellationToken);
        return ncrs ?? new List<NcrDto>();
    }

    /// <inheritdoc />
    public async Task<NcrDto?> GetNcrAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/engine/v1/ncr/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NcrDto>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateNcrAsync(CreateNcrRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/engine/v1/ncr", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateNcrResponse>(cancellationToken: cancellationToken);
        return result?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task UpdateNcrStatusAsync(
        Guid id,
        NcrStatus status,
        DateTime? dueDate = null,
        DateTime? closedAt = null,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateNcrStatusRequest
        {
            Status = status,
            DueDate = dueDate,
            ClosedAt = closedAt
        };

        var response = await _httpClient.PutAsJsonAsync($"/engine/v1/ncr/{id}/status", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NcrStatusHistoryDto>> GetNcrHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/engine/v1/ncr/{id}/history", cancellationToken);
        response.EnsureSuccessStatusCode();

        var history = await response.Content.ReadFromJsonAsync<List<NcrStatusHistoryDto>>(cancellationToken: cancellationToken);
        return history ?? new List<NcrStatusHistoryDto>();
    }
}

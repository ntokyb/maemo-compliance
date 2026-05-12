using System.Net.Http.Json;
using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Implementation of the Risks client for the Maemo Compliance Engine API.
/// </summary>
public class RisksClient : IRisksClient
{
    private readonly HttpClient _httpClient;

    internal RisksClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RiskDto>> GetRisksAsync(
        RiskCategory? category = null,
        RiskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (category.HasValue)
            queryParams.Add($"category={category.Value}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/engine/v1/risks{queryString}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var risks = await response.Content.ReadFromJsonAsync<List<RiskDto>>(cancellationToken: cancellationToken);
        return risks ?? new List<RiskDto>();
    }

    /// <inheritdoc />
    public async Task<RiskDto?> GetRiskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/engine/v1/risks/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RiskDto>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateRiskAsync(CreateRiskRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/engine/v1/risks", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateRiskResponse>(cancellationToken: cancellationToken);
        return result?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task UpdateRiskAsync(Guid id, UpdateRiskRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/engine/v1/risks/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

using System.Net.Http.Json;
using Maemo.Engine.Client.Models;

namespace Maemo.Engine.Client;

/// <summary>
/// Implementation of the Documents client for the Maemo Compliance Engine API.
/// </summary>
public class DocumentsClient : IDocumentsClient
{
    private readonly HttpClient _httpClient;

    internal DocumentsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(
        DocumentStatus? status = null,
        string? department = null,
        bool includeAllVersions = false,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");
        if (!string.IsNullOrWhiteSpace(department))
            queryParams.Add($"department={Uri.EscapeDataString(department)}");
        if (includeAllVersions)
            queryParams.Add("includeAllVersions=true");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/engine/v1/documents{queryString}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var documents = await response.Content.ReadFromJsonAsync<List<DocumentDto>>(cancellationToken: cancellationToken);
        return documents ?? new List<DocumentDto>();
    }

    /// <inheritdoc />
    public async Task<DocumentDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/engine/v1/documents/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/engine/v1/documents", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateDocumentResponse>(cancellationToken: cancellationToken);
        return result?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/engine/v1/documents/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<Guid> CreateDocumentVersionAsync(Guid id, CreateNewDocumentVersionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/engine/v1/documents/{id}/versions", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateDocumentResponse>(cancellationToken: cancellationToken);
        return result?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task ChangeDocumentStatusAsync(Guid id, DocumentStatus status, string? approverUserId = null, CancellationToken cancellationToken = default)
    {
        var request = new ChangeDocumentStatusRequest
        {
            Status = status,
            ApproverUserId = approverUserId
        };

        var response = await _httpClient.PutAsJsonAsync($"/engine/v1/documents/{id}/status", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

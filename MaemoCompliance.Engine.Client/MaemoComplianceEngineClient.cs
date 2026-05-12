using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Main client for interacting with the Maemo Compliance Engine API.
/// Provides access to all engine modules: Documents, NCR, Risks, and Audits.
/// </summary>
public class MaemoComplianceEngineClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly MaemoComplianceEngineClientOptions _options;
    private bool _disposed;

    /// <summary>
    /// Gets the Documents client for document management operations.
    /// </summary>
    public IDocumentsClient Documents { get; }

    /// <summary>
    /// Gets the NCR client for Non-Conformance Report operations.
    /// </summary>
    public INcrClient Ncr { get; }

    /// <summary>
    /// Gets the Risks client for risk register operations.
    /// </summary>
    public IRisksClient Risks { get; }

    /// <summary>
    /// Gets the Audit client for audit management operations.
    /// </summary>
    public IAuditClient Audit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaemoComplianceEngineClient"/> class.
    /// </summary>
    /// <param name="options">The client configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when BaseUrl or ApiKey is null or empty.</exception>
    public MaemoComplianceEngineClient(MaemoComplianceEngineClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new ArgumentException("BaseUrl is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("ApiKey is required.", nameof(options));

        _options = options;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/')),
            Timeout = options.Timeout ?? TimeSpan.FromSeconds(30)
        };

        // Set API key header
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Initialize sub-clients
        Documents = new DocumentsClient(_httpClient);
        Ncr = new NcrClient(_httpClient);
        Risks = new RisksClient(_httpClient);
        Audit = new AuditClient(_httpClient);
    }

    /// <summary>
    /// Disposes the HTTP client and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Configuration options for the Maemo Compliance Engine client.
/// </summary>
public class MaemoComplianceEngineClientOptions
{
    /// <summary>
    /// The base URL of the Maemo Compliance Engine API (e.g., "https://api.maemo.com").
    /// </summary>
    public string BaseUrl { get; set; } = null!;

    /// <summary>
    /// The API key for authentication. This will be sent in the X-Api-Key header.
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// Optional timeout for HTTP requests. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

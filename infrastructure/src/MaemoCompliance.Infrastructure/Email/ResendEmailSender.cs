using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaemoCompliance.Infrastructure.Email;

/// <summary>
/// Sends email via the Resend HTTP API (<see href="https://resend.com/docs/api-reference/emails/send-email"/>).
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private const string EmailsEndpoint = "https://api.resend.com/emails";

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IHttpClientFactory httpClientFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Resend: skipped send — empty recipient (subject {Subject}).", subject);
            return;
        }

        var apiKey = _options.ApiKey;
        var from = _options.FromAddress;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning(
                "Resend: ApiKey or FromAddress missing; skipping send to {To} (subject {Subject}).",
                toEmail,
                subject);
            return;
        }

        var payload = new Dictionary<string, object?>
        {
            ["from"] = from.Trim(),
            ["to"] = new[] { toEmail.Trim() },
            ["subject"] = subject,
            ["html"] = body ?? string.Empty,
        };

        var json = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, EmailsEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var http = _httpClientFactory.CreateClient("Resend");
        var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Resend API error {Status}: {Body}", response.StatusCode, errBody);
            throw new InvalidOperationException($"Failed to send email via Resend: {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? messageId = null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                messageId = idProp.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resend: could not parse message id from response");
        }

        _logger.LogInformation(
            "Email sent via Resend to {To} — subject: {Subject}, id: {Id}",
            toEmail,
            subject,
            messageId);
    }
}

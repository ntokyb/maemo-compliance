namespace Maemo.Shared.Contracts.Common;

/// <summary>
/// Standardized error response model for all API surfaces (Engine, Portal, Admin).
/// </summary>
public sealed record ErrorResponse(
    string Code,
    string Message,
    string? Detail = null,
    string? CorrelationId = null
);


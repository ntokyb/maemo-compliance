using Maemo.Shared.Contracts.Common;

namespace Maemo.Admin.Api.Common;

/// <summary>
/// Helper methods for returning standardized error responses.
/// </summary>
public static class ErrorResults
{
    /// <summary>
    /// Returns a BadRequest (400) error response.
    /// </summary>
    public static IResult BadRequest(string code, string message, string? detail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.BadRequest(new ErrorResponse(code, message, detail, correlationId));
    }

    /// <summary>
    /// Returns a NotFound (404) error response.
    /// </summary>
    public static IResult NotFound(string code, string message, string? detail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.NotFound(new ErrorResponse(code, message, detail, correlationId));
    }

    /// <summary>
    /// Returns an Unauthorized (401) error response.
    /// </summary>
    public static IResult Unauthorized(string code = "Unauthorized", string message = "You are not authorized to perform this action.", string? detail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.Json(new ErrorResponse(code, message, detail, correlationId), statusCode: StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Returns a Forbidden (403) error response.
    /// </summary>
    public static IResult Forbidden(string code = "Forbidden", string message = "You do not have permission to perform this action.", string? detail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.Json(new ErrorResponse(code, message, detail, correlationId), statusCode: StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Returns a ModuleNotEnabled (403) error response.
    /// </summary>
    public static IResult ModuleNotEnabled(string moduleName)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.Json(
            new ErrorResponse(
                Code: "ModuleNotEnabled",
                Message: $"The {moduleName} module is not enabled for this tenant.",
                Detail: $"Module '{moduleName}' is not included in the tenant's ModulesEnabled list.",
                CorrelationId: correlationId
            ),
            statusCode: StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Returns a Conflict (409) error response.
    /// </summary>
    public static IResult Conflict(string code, string message, string? detail = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        return Results.Json(new ErrorResponse(code, message, detail, correlationId), statusCode: StatusCodes.Status409Conflict);
    }
}


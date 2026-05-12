using Maemo.Application.Common;
using Maemo.Shared.Contracts.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maemo.Api.Middleware;

/// <summary>
/// Global exception handler middleware that standardizes error responses across all API surfaces.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        var path = context.Request.Path.Value ?? "";
        
        // Log the exception with correlation ID
        _logger.LogError(
            exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}",
            correlationId,
            path);

        // Determine error code and message based on exception type
        var (code, message, statusCode) = MapExceptionToErrorResponse(exception);

        // Build error response
        var errorResponse = new ErrorResponse(
            Code: code,
            Message: message,
            Detail: _environment.IsDevelopment() ? exception.ToString() : null,
            CorrelationId: correlationId
        );

        // Set response properties
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Serialize and write response
        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static (string Code, string Message, HttpStatusCode StatusCode) MapExceptionToErrorResponse(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => ("NotFound", "The requested resource was not found.", HttpStatusCode.NotFound),
            ConflictException => ("Conflict", exception.Message, HttpStatusCode.Conflict),
            ArgumentException => ("InvalidArgument", exception.Message, HttpStatusCode.BadRequest),
            InvalidOperationException => ("InvalidOperation", exception.Message, HttpStatusCode.BadRequest),
            UnauthorizedAccessException => ("Unauthorized", "You are not authorized to perform this action.", HttpStatusCode.Unauthorized),
            NotImplementedException => ("NotImplemented", "This feature is not yet implemented.", HttpStatusCode.NotImplemented),
            TimeoutException => ("Timeout", "The operation timed out.", HttpStatusCode.RequestTimeout),
            _ => ("InternalError", "An unexpected error occurred. Please try again later.", HttpStatusCode.InternalServerError)
        };
    }
}


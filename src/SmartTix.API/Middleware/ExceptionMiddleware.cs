using System.Net;
using System.Text.Json;
using SmartTix.Application.Common;

namespace SmartTix.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass request to the next middleware / controller
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception internally
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            // Convert to ApiResponse and write to response
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            // Business rule violations — 400 Bad Request
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),

            // Auth failures — 401 Unauthorized
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),

            // Not found — 404
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),

            // ArgumentException — 400
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),

            // Everything else — 500 Internal Server Error
            // In production, never expose internal error details to the client
            _ => (HttpStatusCode.InternalServerError,
                  _environment.IsDevelopment()
                      ? exception.Message          // show detail in dev
                      : "An unexpected error occurred") // hide detail in prod
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
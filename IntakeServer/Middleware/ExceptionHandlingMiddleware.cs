using IntakeServer.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IntakeServer.Middleware;

/// <summary>
/// Converts domain exceptions into consistent RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            (int statusCode, string title) = exception switch
            {
                InvoiceValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                InvoiceNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                DuplicateInvoiceException => (StatusCodes.Status409Conflict, "Duplicate invoice"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "Something went wrong while processing the request."
                    : exception.Message,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

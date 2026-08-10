using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictor.WebApi.Infrastructure;

/// <summary>
/// Handles unhandled exceptions globally and returns a standardized <see cref="ProblemDetails"/> response.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle an unhandled exception and write a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// <c>true</c> if the exception was handled and a response was written; otherwise, <c>false</c>.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name, exception.Message);

        var environment = httpContext.RequestServices.GetService<IHostEnvironment>();
        var isDevelopment = environment?.IsDevelopment() == true;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure",
            // In production, provide a safe generic message; in development, include exception details for debugging
            Detail = isDevelopment
                ? exception.Message
                : "An unexpected error occurred. Please try again or contact support if the problem persists."
        };

        // Include diagnostic details only in development
        if (isDevelopment)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = exception.InnerException.Message;
            }
        }

        // Always include a safe user-friendly message in extensions
        problemDetails.Extensions["userMessage"] = isDevelopment
            ? exception.Message
            : "An unexpected error occurred. Please try again or contact support if the problem persists.";

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

using SharedKernel;

namespace F1Predictor.WebApi.Infrastructure;

/// <summary>
/// Provides custom result helpers for mapping <see cref="Result"/> errors to HTTP problem responses.
/// </summary>
public static class CustomResults
{
    /// <summary>
    /// Converts a failed <see cref="Result"/> to an HTTP problem response.
    /// </summary>
    /// <param name="result">The result containing the error information.</param>
    /// <returns>
    /// An <see cref="IResult"/> representing the HTTP problem response.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provided <paramref name="result"/> is successful.
    /// </exception>
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        return Results.Problem(
            title: GetTitle(result.Error),
            detail: GetDetail(result.Error),
            type: GetTypeUri(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetErrors(result));
    }

    /// <summary>
    /// Gets the title for the problem response based on the error type.
    /// </summary>
    /// <param name="error">The error instance.</param>
    /// <returns>The title string.</returns>
    static string GetTitle(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.Code,
            ErrorType.Problem => error.Code,
            ErrorType.Failure => error.Code,
            ErrorType.NotFound => error.Code,
            ErrorType.Conflict => error.Code,
            _ => "Server failure"
        };

    /// <summary>
    /// Gets the detail message for the problem response based on the error type.
    /// </summary>
    /// <param name="error">The error instance.</param>
    /// <returns>The detail string.</returns>
    static string GetDetail(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.Description,
            ErrorType.Problem => error.Description,
            ErrorType.Failure => error.Description,
            ErrorType.NotFound => error.Description,
            ErrorType.Conflict => error.Description,
            _ => "An unexpected error occurred"
        };

    /// <summary>
    /// Gets the RFC type URI for the problem response based on the error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The type URI string.</returns>
    static string GetTypeUri(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Failure => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

    /// <summary>
    /// Gets the HTTP status code for the problem response based on the error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>The HTTP status code.</returns>
    static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or ErrorType.Problem or ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>
    /// Gets additional error details for validation errors and includes userMessage for all errors.
    /// </summary>
    /// <param name="result">The result instance.</param>
    /// <returns>
    /// A dictionary containing validation errors or userMessage, or <c>null</c> if neither applies.
    /// </returns>
    static Dictionary<string, object?>? GetErrors(Result result)
    {
        var extensions = new Dictionary<string, object?>();

        // For validation errors, include the full errors array with code, description, and userMessage
        if (result.Error is ValidationError validationError)
        {
            // Project to an explicit anonymous-object shape so serialization is
            // consistent regardless of the global JsonSerializerOptions policy.
            // ApiErrorParser on the client reads "description" and "userMessage".
            var projected = validationError.Errors
                .Select(e => new
                {
                    code = e.Code,
                    description = e.Description,
                    userMessage = e.UserMessage
                })
                .ToArray();

            extensions["errors"] = projected;
        }

        // For all errors (validation and single errors), include the userMessage for client-side consumption
        extensions["userMessage"] = result.Error.UserMessage;

        return extensions.Count > 0 ? extensions : null;
    }
}

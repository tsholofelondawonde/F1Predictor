using System.Text.Json.Serialization;

namespace SharedKernel;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure,
        "A required value was missing. Please check your input and try again.");

    public Error(string code, string description, ErrorType type)
        : this(code, description, type, description) 
    {
    }

    [JsonConstructor]
    public Error(string code, string description, ErrorType type, string userMessage)
    {
        Code = code;
        Description = description;
        Type = type;
        UserMessage = userMessage ?? description;
    }

    /// <summary>
    /// Creates a validation failure error with the specified code and message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="userMessage">The user-friendly message.</param>
    /// <returns>An Error instance representing a validation failure.</returns>
    public static Error ValidationFailure(string code, string message, string? userMessage = null)
    {
        return new Error(code, message, ErrorType.Failure, userMessage ?? message);
    }

    /// <summary>
    /// Creates a validation failure error from an existing error, forcing the type to Failure.
    /// </summary>
    /// <param name="error">The error instance.</param>
    /// <returns>An Error instance representing a validation failure.</returns>
    public static Error ValidationFailure(Error error)
    {
        return new Error(error.Code, error.Description, ErrorType.Failure, error.UserMessage);
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    /// <summary>
    /// A user-friendly, localizable error message for display in the UI.
    /// </summary>
    public string UserMessage { get; }

    public static Error Failure(string code, string description, string? userMessage = null) =>
        new(code, description, ErrorType.Failure, userMessage ?? description);

    public static Error NotFound(string code, string description, string? userMessage = null) =>
        new(code, description, ErrorType.NotFound, userMessage ?? description);

    public static Error Problem(string code, string description, string? userMessage = null) =>
        new(code, description, ErrorType.Problem, userMessage ?? description);

    public static Error Conflict(string code, string description, string? userMessage = null) =>
        new(code, description, ErrorType.Conflict, userMessage ?? description);
}
using F1Predictor.Application.Abstractions.Messaging;
using FluentValidation;
using FluentValidation.Results;
using SharedKernel;

namespace F1Predictor.Application.Abstractions.Behaviours;

    /// <summary>
    /// Provides validation decorators for command handlers.
    /// </summary>
    internal static class ValidationDecorator
{
    /// <summary>
    /// Decorator for <see cref="ICommandHandler{TCommand, TResponse}"/> that performs validation before handling the command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Validates the command and handles it if validation passes; otherwise returns a validation error.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(command, validators);

            if (validationFailures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Failure<TResponse>(CreateValidationError(validationFailures));
        }
    }

    /// <summary>
    /// Decorator for <see cref="ICommandHandler{TCommand}"/> that performs validation before handling the command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Validates the command and handles it if validation passes; otherwise returns a validation error.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(command, validators);

            if (validationFailures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Failure(CreateValidationError(validationFailures));
        }
    }

    /// <summary>
    /// Validates the specified command using the provided validators.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <param name="validators">The validators to use.</param>
    /// <returns>An array of <see cref="ValidationFailure"/> representing validation errors.</returns>
    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TCommand>(command);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context)));

        ValidationFailure[] validationFailures = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToArray();

        return validationFailures;
    }

    /// <summary>
    /// Creates a <see cref="ValidationError"/> from an array of <see cref="ValidationFailure"/>.
    /// </summary>
    /// <param name="validationFailures">The validation failures to include.</param>
    /// <returns>A <see cref="ValidationError"/> containing the specified failures.</returns>
    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new(validationFailures.Select(f => Error.Problem(f.ErrorCode, f.ErrorMessage)).ToArray());
}
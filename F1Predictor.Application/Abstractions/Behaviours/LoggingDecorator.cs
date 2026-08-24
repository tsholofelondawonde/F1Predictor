using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace F1Predictor.Application.Abstractions.Behaviours;

/// <summary>
/// Provides logging decorators for command and query handlers.
/// </summary>
internal static class LoggingDecorator
{
    /// <summary>
    /// Decorator for <see cref="ICommandHandler{TCommand, TResponse}"/> that adds logging for command processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the command and logs processing and completion events.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError(
                    "Completed command {Command} with error {ErrorCode}: {ErrorDescription}",
                    commandName, result.Error.Code, result.Error.Description);
            }

            return result;
        }
    }

    /// <summary>
    /// Decorator for <see cref="ICommandHandler{TCommand}"/> that adds logging for command processing.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles the command and logs processing and completion events.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError(
                    "Completed command {Command} with error {ErrorCode}: {ErrorDescription}",
                    commandName, result.Error.Code, result.Error.Description);
            }

            return result;
        }
    }

    /// <summary>
    /// Decorator for <see cref="IQueryHandler{TQuery, TResponse}"/> that adds logging for query processing.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        /// <summary>
        /// Handles the query and logs processing and completion events.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the query execution.</returns>
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            string queryName = typeof(TQuery).Name;

            logger.LogInformation("Processing query {Query}", queryName);

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed query {Query}", queryName);
            }
            else
            {
                logger.LogError(
                    "Completed query {Query} with error {ErrorCode}: {ErrorDescription}",
                    queryName, result.Error.Code, result.Error.Description);
            }

            return result;
        }
    }
}

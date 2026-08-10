using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using System.Collections.Concurrent;

namespace F1Predictor.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches domain events to their corresponding handlers using dependency injection.
/// Preserves tenant context across service scope boundaries so domain event handlers
/// can access tenant-scoped data with the same access level as the originating request.
/// </summary>
internal sealed class DomainEventsDispatcher(
    IServiceProvider serviceProvider) : IDomainEventsDispatcher
{
    /// <summary>
    /// Caches the mapping between domain event types and their handler interface types.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();

    /// <summary>
    /// Caches the mapping between domain event types and their handler wrapper types.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    /// <summary>
    /// Dispatches the provided domain events to their registered handlers asynchronously.
    /// Extracts the current tenant context and replays it in new service scopes so handlers
    /// can access tenant-scoped data without triggering "not initialized" errors.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to dispatch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

            Type domainEventType = domainEvent.GetType();
            Type handlerType = HandlerTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (object? handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                await handlerWrapper.Handle(domainEvent, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Abstract base class for wrapping domain event handlers.
    /// </summary>
    private abstract class HandlerWrapper
    {
        /// <summary>
        /// Handles the specified domain event asynchronously.
        /// </summary>
        /// <param name="domainEvent">The domain event to handle.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a handler wrapper for the specified handler and domain event type.
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="domainEventType">The type of the domain event.</param>
        /// <returns>A <see cref="HandlerWrapper"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the wrapper cannot be created.</exception>
        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            var instance = Activator.CreateInstance(wrapperType, handler);
            if (instance is null)
            {
                throw new InvalidOperationException($"Could not create HandlerWrapper for type {wrapperType.FullName}.");
            }
            return (HandlerWrapper)instance;
        }
    }

    /// <summary>
    /// Generic handler wrapper for a specific domain event type.
    /// </summary>
    /// <typeparam name="T">The type of the domain event.</typeparam>
    /// <param name="handler">The domain event handler instance.</param>
    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : IDomainEvent
    {
        /// <summary>
        /// The domain event handler for type <typeparamref name="T"/>.
        /// </summary>
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        /// <summary>
        /// Handles the specified domain event asynchronously.
        /// </summary>
        /// <param name="domainEvent">The domain event to handle.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.Handle((T)domainEvent, cancellationToken);
        }
    }
}

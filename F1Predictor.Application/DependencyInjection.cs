using F1Predictor.Application.Abstractions.Behaviours;
using F1Predictor.Application.Abstractions.Messaging;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace F1Predictor.Application;

/// <summary>
/// Provides extension methods for registering application services and handlers in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application services, handlers, decorators, and validators in the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> instance for accessing configuration settings.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var assembly = typeof(DependencyInjection).Assembly;

        // Register query, command and domain event handlers from this assembly
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        // Apply validation decorators first so logging wraps validated handlers
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));

        // Apply logging decorators
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));

        // Only decorate ICommandHandler<> (no-result variant) if implementations exist
        if (services.Any(d => d.ServiceType.IsConstructedGenericType &&
                              d.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
        {
            services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));
            services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
        }

        // Register validators from this assembly (including internal types)
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Backs the championship simulation cache. The dashboard polls the forecast on a timer
        // and two use cases share the same run, so without this the same ten thousand seasons
        // would be simulated several times a minute for an answer that only moves when a race is
        // ingested. See Features/Championship/CachedForecast.
        services.AddHybridCache();

        return services;
    }
}

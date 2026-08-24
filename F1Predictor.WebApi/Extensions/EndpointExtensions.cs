using F1Predictor.WebApi.Endpoints;
using F1Predictor.WebApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace F1Predictor.WebApi.Extensions;


/// <summary>
/// Provides extension methods for registering and mapping endpoints in the application.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Registers all non-abstract, non-interface types in the specified assembly that implement <see cref="IEndpoint"/>
    /// as transient services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the endpoints to.</param>
    /// <param name="assembly">The assembly to scan for endpoint implementations.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    /// <summary>
    /// Maps all registered <see cref="IEndpoint"/> instances to the application's route builder.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance.</param>
    /// <param name="routeGroupBuilder">An optional <see cref="RouteGroupBuilder"/> to use for mapping endpoints. If null, <paramref name="app"/> is used.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }

    /// <summary>
    /// Requires a valid <c>X-Api-Key</c> header on the route, via <see cref="ApiKeyEndpointFilter"/>.
    /// </summary>
    /// <param name="app">The <see cref="RouteHandlerBuilder"/> to apply the filter to.</param>
    /// <returns>The updated <see cref="RouteHandlerBuilder"/>.</returns>
    public static RouteHandlerBuilder RequireApiKey(this RouteHandlerBuilder app)
    {
        return app.AddEndpointFilter<ApiKeyEndpointFilter>();
    }
}

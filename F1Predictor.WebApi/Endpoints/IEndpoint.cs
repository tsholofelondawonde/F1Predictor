namespace F1Predictor.WebApi.Endpoints;

/// <summary>
/// Defines a contract for mapping an endpoint to the application's route builder.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the endpoint to the specified <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The route builder to which the endpoint will be mapped.</param>
    void MapEndpoint(IEndpointRouteBuilder app);
}

namespace F1Predictor.WebApi.Extensions;


/// <summary>
/// Provides extension methods for configuring the application's middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Note: This method is deprecated in favor of the modern .NET OpenAPI approach.
    /// OpenAPI documents are now served via MapOpenApi() and can be viewed directly at /openapi/v1.json
    /// For a UI, consider using Scalar or other compatible OpenAPI viewers.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>The configured <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder MapOpenApiWithScalar(this WebApplication app)
    {
        return app;
    }
}

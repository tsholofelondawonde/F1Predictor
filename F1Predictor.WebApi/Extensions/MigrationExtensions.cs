using F1Predictor.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.WebApi.Extensions;


/// <summary>
/// Provides extension methods for applying database migrations at application startup.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies any pending migrations for the application's database context.
    /// This ensures the database schema is up to date with the current model.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }
}

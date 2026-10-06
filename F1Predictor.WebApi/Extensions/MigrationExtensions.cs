using F1Predictor.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace F1Predictor.WebApi.Extensions;


/// <summary>
/// Provides extension methods for applying database migrations at application startup.
/// </summary>
public static class MigrationExtensions
{
    private const int MaxAttempts = 5;

    /// <summary>
    /// Applies any pending migrations for the application's database context.
    /// This ensures the database schema is up to date with the current model.
    /// </summary>
    /// <remarks>
    /// Transient connection failures (a Wi-Fi drop, a laptop resuming from sleep) are retried
    /// with exponential backoff rather than crashing startup. The DbContext deliberately has no
    /// <c>EnableRetryOnFailure</c> — a retrying execution strategy rejects the manual transaction
    /// in <c>IngestSeasonCommandHandler</c> — so the retry lives here, at the one call site that
    /// needs it. Non-transient failures and the final attempt still throw.
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(MigrationExtensions));

        for (int attempt = 1; attempt < MaxAttempts; attempt++)
        {
            try
            {
                dbContext.Database.Migrate();
                return;
            }
            catch (NpgsqlException ex) when (ex.IsTransient)
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.LogWarning(
                    ex,
                    "Applying migrations failed on attempt {Attempt}/{MaxAttempts}; retrying in {Delay}s.",
                    attempt, MaxAttempts, delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }

        // Final attempt: let any failure propagate and stop startup.
        dbContext.Database.Migrate();
    }
}

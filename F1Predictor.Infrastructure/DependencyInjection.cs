using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.OpenF1;
using F1Predictor.Infrastructure.Database;
using F1Predictor.Infrastructure.DomainEvents;
using F1Predictor.Infrastructure.Legacy;
using F1Predictor.Infrastructure.MachineLearning;
using F1Predictor.Infrastructure.OpenF1;
using F1Predictor.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace F1Predictor.Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services, including database, authentication, authorization, and health checks.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddOpenF1(configuration)
            .AddMachineLearning(configuration)
            .AddHealthChecks(configuration);
     
    /// <summary>
    /// Registers core infrastructure services and repositories.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.AddScoped<ILegacyDatabaseImporter, LegacySqliteImporter>();

        return services;
    }

    /// <summary>
    /// Registers the typed OpenF1 client with a politeness delay and the standard resilience
    /// pipeline, which is what handles the 429s a full-season ingest will inevitably provoke.
    /// </summary>
    private static IServiceCollection AddOpenF1(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenF1Options>(configuration.GetSection(OpenF1Options.SectionName));

        services.AddHttpClient<IOpenF1Client, OpenF1HttpClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OpenF1Options>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddHttpMessageHandler(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OpenF1Options>>().Value;
            return new PolitenessDelayHandler(TimeSpan.FromMilliseconds(options.RequestDelayMilliseconds));
        })
        .AddStandardResilienceHandler(resilience =>
        {
            // A season ingest is dozens of sequential calls to a free API; give it room to
            // back off rather than failing the whole run on one throttled response.
            resilience.Retry.MaxRetryAttempts = 5;
            resilience.Retry.UseJitter = true;
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
        });

        return services;
    }

    /// <summary>
    /// Registers model training and serving. Both are singletons: the trainer holds an
    /// <c>MLContext</c>, and the predictor caches loaded models between requests.
    /// </summary>
    private static IServiceCollection AddMachineLearning(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ModelStorageOptions>(configuration.GetSection(ModelStorageOptions.SectionName));

        services.AddSingleton<IModelTrainer, MlNetModelTrainer>();
        services.AddSingleton<IRacePredictor, MlNetRacePredictor>();

        return services;
    }

    /// <summary>
    /// Configures and registers the application's database context with connection resilience.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("ProdDb")
            ?? configuration.GetConnectionString("LocalDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string found. Ensure either 'ConnectionStrings:ProdDb' " +
                "(for production) or 'ConnectionStrings:LocalDb' (for development) is configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default);
                npgsqlOptions.CommandTimeout(60);
            });
            options.EnableSensitiveDataLogging(false)
                   .EnableDetailedErrors(false);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        return services;
    }

    /// <summary>
    /// Registers health check services for the application.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProdDb")
            ?? configuration.GetConnectionString("LocalDb");

        var healthChecksBuilder = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecksBuilder.AddNpgSql(connectionString);
        }

        return services;
    }
}

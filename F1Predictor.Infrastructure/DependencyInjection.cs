using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Headers;
using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Legacy;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.OpenF1;
using F1Predictor.Infrastructure.AI;
using F1Predictor.Infrastructure.Database;
using F1Predictor.Infrastructure.DomainEvents;
using F1Predictor.Infrastructure.Ingestion;
using F1Predictor.Infrastructure.Legacy;
using F1Predictor.Infrastructure.MachineLearning;
using F1Predictor.Infrastructure.OpenF1;
using F1Predictor.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Quartz;
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
            .AddIngestionScheduler(configuration)
            .AddAi(configuration)
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
    /// Registers the Quartz job that keeps a season's data current without a human re-running
    /// ingest — see <see cref="SeasonIngestionCoordinatorJob"/> for how it decides when to.
    /// </summary>
    private static IServiceCollection AddIngestionScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IngestionSchedulerOptions>(configuration.GetSection(IngestionSchedulerOptions.SectionName));

        // The trigger's interval has to be known at schedule-build time below, so it's read
        // eagerly here in addition to the IOptions binding the job itself uses.
        var schedulerOptions = configuration.GetSection(IngestionSchedulerOptions.SectionName)
            .Get<IngestionSchedulerOptions>() ?? new IngestionSchedulerOptions();

        if (!schedulerOptions.Enabled)
        {
            return services;
        }

        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(SeasonIngestionCoordinatorJob));
            quartz.AddJob<SeasonIngestionCoordinatorJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInMinutes(schedulerOptions.CoordinatorIntervalMinutes)
                    .RepeatForever())
                .StartNow());
        });
        services.AddQuartzHostedService(quartz => quartz.WaitForJobsToComplete = true);

        return services;
    }

    /// <summary>
    /// Registers the chat client behind <c>IChatClient</c>. With <c>Ai:Provider=None</c> (the
    /// default, and production) an <see cref="UnavailableChatClient"/> is registered so nothing
    /// fails to resolve; handlers consult <see cref="IAiCapabilities"/> before calling it.
    /// </summary>
    internal static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddSingleton<IAiCapabilities, AiCapabilities>();

        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        services.AddOllamaHttpClient(options);
        services.AddChatClientPipeline(options);

        if (options.Provider == AiProvider.Ollama)
        {
            services.AddHealthChecks().AddCheck<OllamaHealthCheck>("ai", failureStatus: HealthStatus.Degraded, tags: ["ai"]);
        }
        else if (options.Provider == AiProvider.OpenAi)
        {
            services.AddOpenAiHttpClient(options);
            services.AddHealthChecks().AddCheck<OpenAiHealthCheck>("ai", failureStatus: HealthStatus.Degraded, tags: ["ai"]);
        }

        return services;
    }

    private static void AddOllamaHttpClient(this IServiceCollection services, AiOptions options)
    {
        // No resilience handler here on purpose: one generation can legitimately take a minute
        // on an 8B model, and a retry would only start it again. Note that Program.cs currently
        // has builder.AddServiceDefaults() commented out; if it is ever re-enabled, its global
        // AddStandardResilienceHandler would wrap this client too, adding retries and a 30 s
        // per-attempt timeout underneath the 120 s TimeoutSeconds below — at which point this
        // client needs .RemoveAllResilienceHandlers() to keep behaving as it does today.
        services.AddHttpClient(OllamaHealthCheck.HttpClientName, client =>
        {
            // S1075 false-positives on the trailing-slash literal: without it, Uri's RFC 3986
            // merge drops the last path segment of a configured endpoint (e.g. "/ollama") when
            // combining with a relative request, silently breaking any non-root endpoint.
#pragma warning disable S1075 // "/" here is a URI path-segment separator, not a hardcoded path.
            client.BaseAddress = new Uri(options.Ollama.Endpoint.TrimEnd('/') + "/");
#pragma warning restore S1075
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
    }

    /// <summary>
    /// The OpenAI SDK client manages its own <c>HttpClient</c> with its own defaults (a 100 s
    /// network timeout and its own retry policy) — <see cref="AddOpenAiHttpClient"/>'s named
    /// client is only used by the health check. Without this, the chat client would silently
    /// ignore <see cref="AiOptions.TimeoutSeconds"/> and retry a hung generation, both contrary
    /// to the no-retry-on-a-slow-generation stance documented on <see cref="AiOptions"/>.
    /// </summary>
    private static OpenAIClientOptions BuildOpenAiClientOptions(AiOptions options)
    {
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };

        if (!string.IsNullOrWhiteSpace(options.OpenAi.Endpoint))
        {
            clientOptions.Endpoint = new Uri(options.OpenAi.Endpoint);
        }

        return clientOptions;
    }

    private static void AddOpenAiHttpClient(this IServiceCollection services, AiOptions options)
    {
        services.AddHttpClient(OpenAiHealthCheck.HttpClientName, client =>
        {
            if (!string.IsNullOrWhiteSpace(options.OpenAi.Endpoint))
            {
#pragma warning disable S1075 // see AddOllamaHttpClient above — same trailing-slash reasoning.
                client.BaseAddress = new Uri(options.OpenAi.Endpoint.TrimEnd('/') + "/");
#pragma warning restore S1075
            }
            else
            {
#pragma warning disable S1075 // OpenAI's own default base address, not a project-specific hardcoded path.
                client.BaseAddress = new Uri("https://api.openai.com/v1/");
#pragma warning restore S1075
            }

            if (!string.IsNullOrWhiteSpace(options.OpenAi.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.OpenAi.ApiKey);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
    }

    private static void AddChatClientPipeline(this IServiceCollection services, AiOptions options)
    {
        services.AddChatClient(sp => options.Provider switch
            {
                AiProvider.Ollama => new OllamaApiClient(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHealthCheck.HttpClientName),
                    options.Ollama.Model),
                AiProvider.OpenAi => new OpenAIClient(
                        new ApiKeyCredential(string.IsNullOrWhiteSpace(options.OpenAi.ApiKey) ? "unused" : options.OpenAi.ApiKey),
                        BuildOpenAiClientOptions(options))
                    .GetChatClient(options.OpenAi.Model)
                    .AsIChatClient(),
                _ => new UnavailableChatClient()
            })
            // ChatClientBuilder composes first-added-outermost, so a request runs
            // ConfigureOptions -> FunctionInvocation -> Logging -> OpenTelemetry -> the provider:
            // the sampling defaults are applied once, then the tool-invocation loop sits
            // OUTSIDE logging and tracing, so each model round-trip inside the loop is logged
            // and spanned on its own — which is the order M.E.AI recommends, and why a
            // three-tool question shows up as three spans rather than one.
            .ConfigureOptions(chat =>
            {
                chat.Temperature ??= options.Temperature;

                if (options.Provider == AiProvider.Ollama)
                {
                    // num_ctx is an Ollama-only sampling option; it has no meaning to an
                    // OpenAI-compatible endpoint, so it's only ever set on that path.
                    chat.AdditionalProperties ??= [];
                    chat.AdditionalProperties.TryAdd("num_ctx", options.Ollama.ContextLength);
                }
            })
            .UseFunctionInvocation(configure: invoker =>
            {
                invoker.MaximumIterationsPerRequest = options.MaxToolIterations;
                // The seven tools all query through one scoped DbContext, which is not thread-safe.
                invoker.AllowConcurrentInvocation = false;
            })
            .UseLogging()
            .UseOpenTelemetry();
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

        connectionString = NpgsqlConnectionStrings.RequireSsl(connectionString);

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
            healthChecksBuilder.AddNpgSql(NpgsqlConnectionStrings.RequireSsl(connectionString));
        }

        return services;
    }
}

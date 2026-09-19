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
            .AddScheduler(configuration)
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
    /// Registers Quartz and its jobs. The container itself — <c>AddQuartz</c> and
    /// <c>AddQuartzHostedService</c> — and both <c>AddJob</c> registrations are unconditional, so
    /// <see cref="SeasonIngestionCoordinatorJob"/> can always <c>TriggerJob</c> an
    /// <see cref="AnalysisRefreshJob"/> run on demand even when that job's own interval trigger
    /// (or the ingestion coordinator's) is switched off. Only the two *triggers* below are
    /// conditional:
    /// <list type="bullet">
    /// <item><description><see cref="SeasonIngestionCoordinatorJob"/>'s interval trigger — gated
    /// on <c>IngestionScheduler:Enabled</c>, as before.</description></item>
    /// <item><description><see cref="AnalysisRefreshJob"/>'s interval-backstop trigger — gated on
    /// <c>AnalysisRefresh:Enabled</c> <i>and</i> at least one AI channel (chat or embeddings)
    /// being available, read once here from the same configuration <see cref="AddAi"/> binds
    /// <see cref="AiOptions"/> from. With <c>Ai:Provider=None</c> and no embeddings provider, this
    /// trigger is not registered at all — a bare deployment does nothing new.</description></item>
    /// </list>
    /// </summary>
    private static IServiceCollection AddScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IngestionSchedulerOptions>(configuration.GetSection(IngestionSchedulerOptions.SectionName));
        services.Configure<AnalysisRefreshOptions>(configuration.GetSection(AnalysisRefreshOptions.SectionName));

        // Trigger intervals and gating have to be known at schedule-build time below, so they're
        // read eagerly here in addition to the IOptions bindings the jobs themselves use.
        var schedulerOptions = configuration.GetSection(IngestionSchedulerOptions.SectionName)
            .Get<IngestionSchedulerOptions>() ?? new IngestionSchedulerOptions();
        var analysisRefreshOptions = configuration.GetSection(AnalysisRefreshOptions.SectionName)
            .Get<AnalysisRefreshOptions>() ?? new AnalysisRefreshOptions();
        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        // Mirrors AiCapabilities.ChatAvailable / EmbeddingsAvailable — IAiCapabilities isn't
        // resolvable yet at this point in service registration, so the same two checks are
        // repeated directly against the freshly-bound options here.
        var aiChannelAvailable = aiOptions.Provider is AiProvider.Ollama or AiProvider.OpenAi
            || aiOptions.Embeddings.Provider == EmbeddingsProvider.OpenAi;

        services.AddQuartz(quartz =>
        {
            var ingestionJobKey = new JobKey(nameof(SeasonIngestionCoordinatorJob));
            quartz.AddJob<SeasonIngestionCoordinatorJob>(job => job.WithIdentity(ingestionJobKey));

            if (schedulerOptions.Enabled)
            {
                quartz.AddTrigger(trigger => trigger
                    .ForJob(ingestionJobKey)
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInMinutes(schedulerOptions.CoordinatorIntervalMinutes)
                        .RepeatForever())
                    .StartNow());
            }

            quartz.AddJob<AnalysisRefreshJob>(job => job.WithIdentity(AnalysisRefreshJob.Key));

            if (analysisRefreshOptions.Enabled && aiChannelAvailable)
            {
                quartz.AddTrigger(trigger => trigger
                    .ForJob(AnalysisRefreshJob.Key)
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInMinutes(analysisRefreshOptions.IntervalMinutes)
                        .RepeatForever())
                    .StartNow());
            }
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
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(
                o => o.Provider != AiProvider.OpenAi || !string.IsNullOrWhiteSpace(o.OpenAi.ApiKey),
                "Ai:OpenAi:ApiKey is required when Ai:Provider is OpenAi.")
            .Validate(
                o => o.Embeddings.Provider != EmbeddingsProvider.OpenAi || !string.IsNullOrWhiteSpace(o.OpenAi.ApiKey),
                "Ai:OpenAi:ApiKey is required when Ai:Embeddings:Provider is OpenAi.")
            .ValidateOnStart();

        services.AddSingleton<IAiCapabilities, AiCapabilities>();

        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        services.AddOllamaHttpClient(options);
        services.AddChatAndEmbeddingsPipeline(options);

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

    private static void AddChatAndEmbeddingsPipeline(this IServiceCollection services, AiOptions options)
    {
        // Built once, shared by chat and embeddings when either channel uses OpenAi — a null
        // ApiKey means neither channel selected OpenAi (options validation above would already
        // have failed startup otherwise), so the switches below never dereference a null client.
        OpenAIClient? openAiClient = !string.IsNullOrWhiteSpace(options.OpenAi.ApiKey)
            ? new OpenAIClient(new ApiKeyCredential(options.OpenAi.ApiKey), BuildOpenAiClientOptions(options))
            : null;

        services.AddChatClient(sp => options.Provider switch
            {
                AiProvider.Ollama => new OllamaApiClient(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHealthCheck.HttpClientName),
                    options.Ollama.Model),
                AiProvider.OpenAi => openAiClient!.GetChatClient(options.OpenAi.ChatModel).AsIChatClient(),
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
                chat.MaxOutputTokens ??= options.MaxOutputTokens;

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

        services.AddEmbeddingGenerator(_ => options.Embeddings.Provider switch
            {
                EmbeddingsProvider.OpenAi => openAiClient!.GetEmbeddingClient(options.Embeddings.Model).AsIEmbeddingGenerator(),
                _ => new UnavailableEmbeddingGenerator()
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

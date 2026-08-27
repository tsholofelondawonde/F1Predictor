using F1Predictor.Application;
using F1Predictor.Infrastructure;
using F1Predictor.WebApi;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// GENERATOR_ASPIRE_TOKEN: WITH_ASPIRE
// builder.AddServiceDefaults();

// GENERATOR_ASPIRE_TOKEN: WITHOUT_ASPIRE
builder.Services.AddObservability(builder.Environment, builder.Configuration);

// Azure Container Apps terminates TLS at its ingress and forwards plain HTTP to the container,
// so without this the UseHttpsRedirection below sees scheme "http" on every request and 307s
// forever. The ingress is the only route to the container, and its proxy sits on neither a
// known network nor loopback, so the default proxy allow-list has to be cleared rather than
// extended.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

const string FrontendCorsPolicy = "Frontend";

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:3000")
          .WithMethods("GET", "POST")
          .WithHeaders("Content-Type", "X-Api-Key")));

// Only the four mutating routes that opt into these policies via .RequireRateLimiting(...)
// are affected — every other route is unrestricted.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Without this, a rejection is a bare status code with a zero-length body — and the frontend
    // cannot tell that apart from a dead server: ApiError.fromAxiosError has no body to read, so
    // it reported every rate-limited ingest as "Could not reach the server. Check your connection."
    // Write ProblemDetails and a Retry-After so the caller learns it is throttled, not offline.
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 0;

        if (retryAfterSeconds > 0)
        {
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.29",
                Title = "RateLimit.Exceeded",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = retryAfterSeconds > 0
                    ? $"Too many requests. Try again in {FormatRetryWait(retryAfterSeconds)}."
                    : "Too many requests. Wait a moment and try again."
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    };

    options.AddPolicy(RateLimiterPolicies.Ingest, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimiterPolicies.Mutating, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services
    .AddOpenApi()
    .AddResponseCompression()
    .AddApplication(builder.Configuration)
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// GENERATOR_ASPIRE_TOKEN: WITH_ASPIRE
// app.MapDefaultEndpoints();
// Paired with the commented-out builder.AddServiceDefaults() above — calling this without
// it maps a second "/health" endpoint alongside the app's own MapHealthChecks below and
// makes every request to it fail with AmbiguousMatchException.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Applied on startup rather than by hand: the connection string is injected by the Aspire
    // app host, so `dotnet ef database update` would need it configured a second time.
    //
    // Deliberately not applied outside Development: the container app can scale out, and
    // concurrent replicas racing on Database.Migrate() at startup is worse than migrating
    // out of band. Deployments run `dotnet ef database update` against Neon by hand.
    app.ApplyMigrations();
}

// Must run before UseHsts/UseHttpsRedirection so the forwarded scheme is already applied when
// they decide whether to redirect. See the ForwardedHeadersOptions comment above.
app.UseForwardedHeaders();

// Skipped in Development: the AppHost only pins an HTTP endpoint (see AppHost.cs), so a
// redirect to the launchSettings https profile's port would point at a port nothing is
// bound to under Aspire orchestration.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRequestContextLogging();

app.UseSecurityHeaders();

app.UseResponseCompression();

app.UseRouting();

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

// Mapped in every environment, not just Development: the deployed container is driven from
// Scalar, and it doubles as the deployment smoke test. Nothing here is a mutating route —
// the four that are still sit behind the X-Api-Key filter. SecurityHeadersMiddleware already
// exempts /scalar and /openapi from its strict CSP so the page renders.
app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.WithTitle("Application API")
           .WithTheme(ScalarTheme.Default)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
           .WithOpenApiRoutePattern("/openapi/{documentName}.json");
});

// BUILD_SHA is baked into the image by the Dockerfile's GIT_SHA build arg, so the payload names
// the commit that is actually answering rather than just asserting something is. The deploy
// workflow's smoke test asserts on it: a new revision that fails to provision leaves the previous
// one serving, and "Healthy" on its own would report that as a successful deploy. Outside CI this
// is "unknown", which is honest — a local or Aspire run is not a build.
var buildSha = app.Configuration["BUILD_SHA"] ?? "unknown";

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, buildSha)
});

app.MapEndpoints();


// The Retry-After header carries the machine-readable seconds; this is the same wait phrased
// for a human, so the banner reads "try again in 5 minutes" rather than "in 300 seconds".
static string FormatRetryWait(int seconds)
{
    if (seconds < 60)
    {
        return seconds == 1 ? "1 second" : $"{seconds} seconds";
    }

    var minutes = (int)Math.Ceiling(seconds / 60d);

    return minutes == 1 ? "1 minute" : $"{minutes} minutes";
}

// Health check response writer
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report, string buildSha)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        status = report.Status.ToString(),
        build = buildSha,
        timestamp = DateTime.UtcNow,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    };
    return context.Response.WriteAsJsonAsync(response);
}

await app.RunAsync();

using F1Predictor.Application;
using F1Predictor.Infrastructure;
using F1Predictor.WebApi;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// GENERATOR_ASPIRE_TOKEN: WITH_ASPIRE
// builder.AddServiceDefaults();

// GENERATOR_ASPIRE_TOKEN: WITHOUT_ASPIRE
builder.Services.AddObservability(builder.Environment, builder.Configuration);

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
    app.ApplyMigrations();

    app.MapOpenApi();
}

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

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Application API")
               .WithTheme(ScalarTheme.Default)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
               .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.MapEndpoints();


// Health check response writer
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        status = report.Status.ToString(),
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

using System.Net.ServerSentEvents;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.AskAnalyst;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;

namespace F1Predictor.WebApi.Endpoints.Analysis;

internal sealed class AskAnalyst : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ai/ask", async Task<IResult> (
            AskAnalystCommand command,
            ICommandHandler<AskAnalystCommand, AnalystReply> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            // Proxies (nginx, Azure ingress) may otherwise buffer the whole stream before forwarding it.
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers["X-Accel-Buffering"] = "no";

            return TypedResults.ServerSentEvents(
                result.Value.Events.Select(e => new SseItem<AnalystEvent>(e, e.Type)));
        })
        .WithTags(Tags.Analysis)
        .WithName("AskAnalyst")
        .WithSummary("Ask the AI analyst a question; the answer streams back as server-sent events.")
        .WithDescription(
            "Events: 'status' while a tool runs, 'delta' text fragments, then 'done' — or 'error'. " +
            "Every number comes from the prediction models via tools; the analyst never predicts on its own. " +
            "Requires an AI provider to be configured (see /api/ai/status).")
        .Accepts<AskAnalystCommand>("application/json")
        .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireApiKey()
        .RequireRateLimiting(RateLimiterPolicies.Analyst)
        .RequireRateLimiting(RateLimiterPolicies.AnalystDaily);
    }
}

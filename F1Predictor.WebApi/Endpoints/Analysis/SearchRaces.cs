using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;
using F1Predictor.Application.Features.Analysis.SearchRaces;
using F1Predictor.WebApi.Extensions;
using F1Predictor.WebApi.Infrastructure;
using SharedKernel;

namespace F1Predictor.WebApi.Endpoints.Analysis;

/// <summary>
/// Free-text semantic search over indexed race fact sheets. Unlike
/// <c>GET /api/races/{sessionKey}/similar</c>, which reuses a stored vector, this route makes a
/// fresh embedding call per request — the same cost the analyst chat route bears, and the same
/// reason it sits behind both rate-limiter policies.
/// </summary>
/// <remarks>
/// <see cref="SearchRacesQuery"/> is unvalidated by design (queries carry no FluentValidation
/// validator in this codebase), so <c>q</c>'s length and <c>top</c> are clamped/validated here
/// instead, in the same spirit as <c>AnalystTools.ScenariosAsync</c>'s <c>topN</c> clamp: a
/// too-long or missing <c>top</c> is silently made sane rather than rejected, but a query with no
/// real text to embed is rejected outright, since there is nothing sensible to clamp it to.
/// </remarks>
internal sealed class SearchRaces : IEndpoint
{
    private const int MinTextLength = 3;
    private const int MaxTextLength = 300;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analysis/search", async (
            string? q,
            int? top,
            IQueryHandler<SearchRacesQuery, SimilarRacesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var text = (q ?? string.Empty).Trim();
            if (text.Length < MinTextLength)
            {
                return CustomResults.Problem(Result.Failure(Error.Failure(
                    "Analysis.SearchTextTooShort",
                    $"'q' must be at least {MinTextLength} characters.",
                    $"Enter at least {MinTextLength} characters to search.")));
            }

            if (text.Length > MaxTextLength)
            {
                text = text[..MaxTextLength];
            }

            var clampedTop = Math.Clamp(top ?? 5, 1, 10);

            var result = await handler.Handle(new SearchRacesQuery(text, clampedTop), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Analysis)
        .WithName("SearchRaces")
        .WithSummary("Free-text semantic search over indexed race fact sheets.")
        .WithDescription(
            "Embeds the query text and finds the nearest indexed races by cosine similarity — " +
            "e.g. 'wet race with many retirements'. Costs one embedding call, so this route sits " +
            "behind the same rate limits as the analyst chat.")
        .Produces<SimilarRacesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting(RateLimiterPolicies.Analyst)
        .RequireRateLimiting(RateLimiterPolicies.AnalystDaily);
    }
}

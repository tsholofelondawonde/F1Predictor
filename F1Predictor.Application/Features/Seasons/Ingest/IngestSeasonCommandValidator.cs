using FluentValidation;

namespace F1Predictor.Application.Features.Seasons.Ingest;

internal sealed class IngestSeasonCommandValidator : AbstractValidator<IngestSeasonCommand>
{
    /// <summary>OpenF1's free tier only covers 2023 onwards.</summary>
    private const int EarliestSupportedSeason = 2023;

    public IngestSeasonCommandValidator()
    {
        RuleFor(c => c.Year)
            .GreaterThanOrEqualTo(EarliestSupportedSeason)
            .WithErrorCode("Season.TooEarly")
            .WithMessage($"OpenF1 only serves data from {EarliestSupportedSeason} onwards.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Year)
            .WithErrorCode("Season.InFuture")
            .WithMessage("That season has not happened yet.");
    }
}

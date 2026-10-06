using FluentValidation;

namespace F1Predictor.Application.Features.Analysis.RefreshAnalysis;

internal sealed class RefreshAnalysisCommandValidator : AbstractValidator<RefreshAnalysisCommand>
{
    /// <summary>OpenF1's free tier only covers 2023 onwards.</summary>
    private const int EarliestSupportedSeason = 2023;

    public RefreshAnalysisCommandValidator()
    {
        RuleFor(c => c.Year)
            .GreaterThanOrEqualTo(EarliestSupportedSeason)
            .WithErrorCode("Season.TooEarly")
            .WithMessage($"OpenF1 only serves data from {EarliestSupportedSeason} onwards.")
            .When(c => c.Year is not null);
    }
}

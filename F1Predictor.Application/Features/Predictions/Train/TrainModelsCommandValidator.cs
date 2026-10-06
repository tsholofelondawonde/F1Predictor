using F1Predictor.Domain.Predictions;
using FluentValidation;

namespace F1Predictor.Application.Features.Predictions.Train;

internal sealed class TrainModelsCommandValidator : AbstractValidator<TrainModelsCommand>
{
    /// <summary>OpenF1's free tier only covers 2023 onwards, so no features exist before it.</summary>
    private const int EarliestSupportedSeason = 2023;

    public TrainModelsCommandValidator()
    {
        RuleFor(c => c.Year)
            .GreaterThanOrEqualTo(EarliestSupportedSeason)
            .WithErrorCode("Training.YearTooEarly")
            .WithMessage($"OpenF1 only serves data from {EarliestSupportedSeason} onwards.");

        RuleFor(c => c.FromYear)
            .GreaterThanOrEqualTo(EarliestSupportedSeason)
            .WithErrorCode("Training.FromYearTooEarly")
            .WithMessage($"OpenF1 only serves data from {EarliestSupportedSeason} onwards.")
            .LessThanOrEqualTo(c => c.Year)
            .WithErrorCode("Training.FromYearAfterYear")
            .WithMessage("fromYear must not be later than year.")
            .When(c => c.FromYear.HasValue);

        RuleFor(c => c.Notes)
            .MaximumLength(ModelTrainingRun.NotesMaxLength)
            .WithErrorCode("Training.NotesTooLong");
    }
}

using FluentValidation;

namespace F1Predictor.Application.Features.Analysis.GenerateRacePreview;

internal sealed class GenerateRacePreviewCommandValidator : AbstractValidator<GenerateRacePreviewCommand>
{
    public GenerateRacePreviewCommandValidator()
    {
        RuleFor(c => c.SessionKey)
            .GreaterThan(0)
            .WithErrorCode("Analysis.InvalidSessionKey")
            .WithMessage("A session key must be positive.");
    }
}

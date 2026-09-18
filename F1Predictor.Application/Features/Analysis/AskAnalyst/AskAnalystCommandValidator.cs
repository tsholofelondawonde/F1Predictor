using FluentValidation;

namespace F1Predictor.Application.Features.Analysis.AskAnalyst;

internal sealed class AskAnalystCommandValidator : AbstractValidator<AskAnalystCommand>
{
    private const int EarliestSupportedSeason = 2023;
    private const int MaxTurns = 20;
    private const int MaxTurnLength = 2000;
    private static readonly string[] Roles = ["user", "assistant"];

    public AskAnalystCommandValidator()
    {
        RuleFor(c => c.Year)
            .InclusiveBetween(EarliestSupportedSeason, DateTime.UtcNow.Year + 1)
            .WithErrorCode("Analyst.UnsupportedSeason").WithMessage("Only seasons from 2023 onwards can be discussed.");

        RuleFor(c => c.Messages)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("Analyst.NoMessages").WithMessage("Send at least one message.")
            .Must(m => m.Count <= MaxTurns).WithErrorCode("Analyst.TooManyTurns").WithMessage($"Keep the conversation to {MaxTurns} turns; start a new one.")
            .Must(m => m.Count > 0 && string.Equals(m[^1]?.Role, "user", StringComparison.Ordinal))
                .WithErrorCode("Analyst.LastTurnNotUser").WithMessage("The last message must be from the user.");

        RuleForEach(c => c.Messages).ChildRules(turn =>
        {
            turn.RuleFor(t => t.Role).Must(r => Roles.Contains(r, StringComparer.Ordinal))
                .WithErrorCode("Analyst.InvalidRole").WithMessage("Roles must be 'user' or 'assistant'.");
            turn.RuleFor(t => t.Content).NotEmpty().MaximumLength(MaxTurnLength)
                .WithErrorCode("Analyst.TurnTooLong").WithMessage($"Each message must be 1 to {MaxTurnLength} characters.");
        });
    }
}

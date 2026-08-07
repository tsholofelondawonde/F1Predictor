using FluentValidation;

namespace F1Predictor.Application.Features.Legacy.Import;

internal sealed class ImportLegacyDatabaseCommandValidator : AbstractValidator<ImportLegacyDatabaseCommand>
{
    public ImportLegacyDatabaseCommandValidator()
    {
        RuleFor(c => c.SqlitePath)
            .NotEmpty()
            .WithErrorCode("LegacyImport.PathRequired")
            .WithMessage("A path to the legacy SQLite database is required.");
    }
}

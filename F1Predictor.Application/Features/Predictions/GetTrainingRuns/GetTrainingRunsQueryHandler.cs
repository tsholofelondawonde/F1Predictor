using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Predictions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace F1Predictor.Application.Features.Predictions.GetTrainingRuns;

internal sealed class GetTrainingRunsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetTrainingRunsQuery, TrainingRunsResponse>
{
    private const int MaxTake = 200;

    public async Task<Result<TrainingRunsResponse>> Handle(GetTrainingRunsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<ModelTrainingRun> runs = context.ModelTrainingRuns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Target))
        {
            // Targets are stored by enum name; normalise the caller's casing to match.
            var target = Enum.GetNames<PredictionTarget>()
                .FirstOrDefault(name => string.Equals(name, query.Target.Trim(), StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                return Result.Failure<TrainingRunsResponse>(Error.Problem(
                    "Training.UnknownTarget",
                    $"Unknown target '{query.Target}'. Use Podium or PointsFinish.",
                    "That prediction target doesn't exist. Use Podium or PointsFinish."));
            }

            runs = runs.Where(r => r.Target == target);
        }

        var rows = await runs
            .OrderByDescending(r => r.TrainedAt)
            .ThenByDescending(r => r.Id)
            .Take(Math.Clamp(query.Take, 1, MaxTake))
            .ToListAsync(cancellationToken);

        return Result.Success(new TrainingRunsResponse([.. rows.Select(ToResponse)]));
    }

    private static TrainingRunResponse ToResponse(ModelTrainingRun run) => new(
        run.Id,
        run.TrainedAt,
        run.Target,
        run.FromYear,
        run.Year,
        run.TrainerName,
        run.FeatureNames.Split(',', StringSplitOptions.RemoveEmptyEntries),
        run.TrainingRaceCount,
        run.TrainingRowCount,
        run.ValidationRaceCount,
        run.ValidationRowCount,
        run.ValidationAuc,
        run.ValidationF1,
        run.ValidationLogLoss,
        run.ValidationAuprc,
        run.ValidationPrecision,
        run.ValidationRecall,
        run.BaselineAuc,
        run.ValidationAuc - run.BaselineAuc,
        run.HoldoutSessionKey,
        run.HoldoutAuc,
        run.HoldoutLogLoss,
        run.Notes);
}

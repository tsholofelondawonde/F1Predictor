using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Abstractions.Data;
using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Domain.Analysis;
using Microsoft.Extensions.AI;
using SharedKernel;

namespace F1Predictor.Application.Features.Analysis.IndexRace;

/// <summary>
/// Embeds one classified race's <c>RaceFactSheet</c> and upserts it into
/// <see cref="IRaceEmbeddingIndex"/>, skipping the embedding call entirely when the fact sheet's
/// content hash and the currently configured model both already match what is stored. That hash
/// short-circuit is what lets <c>RefreshAnalysisCommandHandler</c> call this handler for every
/// classified race on every refresh without needing its own staleness check first.
/// </summary>
internal sealed class IndexRaceCommandHandler(
    IApplicationDbContext context,
    IAiCapabilities ai,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IRaceEmbeddingIndex index,
    IDateTimeProvider clock)
    : ICommandHandler<IndexRaceCommand, IndexRaceResponse>
{
    public async Task<Result<IndexRaceResponse>> Handle(IndexRaceCommand command, CancellationToken cancellationToken)
    {
        if (!ai.EmbeddingsAvailable)
        {
            return Result.Failure<IndexRaceResponse>(AnalysisErrors.AiUnavailable);
        }

        var loaded = await RaceIndexInputs.LoadAsync(context, command.SessionKey, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<IndexRaceResponse>(loaded.Error);
        }

        var inputs = loaded.Value;
        var sheet = RaceFactSheet.Build(
            inputs.Meeting, inputs.Session, inputs.Results, inputs.Grid, inputs.PitStops, inputs.Weather, inputs.Directory);

        var existing = await index.GetAsync(command.SessionKey, cancellationToken);
        if (existing is { } current
            && string.Equals(current.ContentHash, sheet.ContentHash, StringComparison.Ordinal)
            && string.Equals(current.Model, ai.EmbeddingModel, StringComparison.Ordinal))
        {
            return Result.Success(new IndexRaceResponse(command.SessionKey, Reindexed: false, sheet.ContentHash));
        }

        var generated = await embeddings.GenerateAsync([sheet.Text], cancellationToken: cancellationToken);
        var vector = generated[0].Vector;

        await index.UpsertAsync(
            new RaceEmbeddingRecord(command.SessionKey, ai.EmbeddingModel!, vector.Length, sheet.ContentHash, sheet.Text, new DateTimeOffset(clock.UtcNow, TimeSpan.Zero)),
            vector, cancellationToken);

        return Result.Success(new IndexRaceResponse(command.SessionKey, Reindexed: true, sheet.ContentHash));
    }
}

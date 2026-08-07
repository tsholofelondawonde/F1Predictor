using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Predictions.RebuildFeatures;

/// <summary>
/// Regenerates the whole feature table from the raw tables.
/// </summary>
/// <remarks>
/// Deliberately a full rebuild rather than an incremental update: at this data volume it costs
/// almost nothing and it rules out a whole class of stale-feature bugs while the engineering
/// rules are still being tuned.
/// </remarks>
public sealed class RebuildFeaturesCommand : ICommand<RebuildFeaturesResponse>;

using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.RefreshAnalysis;

/// <summary>
/// A mutable class rather than a positional record: its only input is a route/body-bound
/// primitive, which minimal-API parameter binding requires a settable property for.
/// </summary>
public sealed class RefreshAnalysisCommand : ICommand<RefreshAnalysisResponse>
{
    /// <summary>A specific season, or null for every year that has ingested data.</summary>
    public int? Year { get; set; }
}

using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Analysis.IndexRace;

/// <summary>
/// A mutable class rather than a positional record: its only input is a route-bound primitive,
/// which minimal-API parameter binding requires a settable property for.
/// </summary>
public sealed class IndexRaceCommand : ICommand<IndexRaceResponse>
{
    public int SessionKey { get; set; }
}

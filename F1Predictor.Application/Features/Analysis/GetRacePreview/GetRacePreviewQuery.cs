using F1Predictor.Application.Abstractions.Messaging;
using F1Predictor.Application.Features.Analysis.GenerateRacePreview;

namespace F1Predictor.Application.Features.Analysis.GetRacePreview;

/// <param name="SessionKey">The race session whose last generated preview should be returned.</param>
public sealed record GetRacePreviewQuery(int SessionKey) : IQuery<RacePreviewResponse>;

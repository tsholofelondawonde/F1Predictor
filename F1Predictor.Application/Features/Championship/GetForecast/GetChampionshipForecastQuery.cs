using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Championship.GetForecast;

/// <summary>
/// Odds of each driver and constructor taking the title, from simulating the rest of the season.
/// </summary>
public sealed record GetChampionshipForecastQuery(int Year) : IQuery<ChampionshipForecastResponse>;

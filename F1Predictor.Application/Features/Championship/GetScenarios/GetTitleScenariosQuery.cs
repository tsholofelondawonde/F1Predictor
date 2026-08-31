using F1Predictor.Application.Abstractions.Messaging;

namespace F1Predictor.Application.Features.Championship.GetScenarios;

/// <summary>
/// What each of the leading contenders needs to happen to take the drivers' title.
/// </summary>
/// <param name="Year">Season to analyse.</param>
/// <param name="TopN">How many drivers from the top of the table to cover.</param>
public sealed record GetTitleScenariosQuery(int Year, int TopN = 5) : IQuery<TitleScenariosResponse>;

using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.AskAnalyst;
using F1Predictor.Application.Features.Analysis.ExplainDriverPrediction;
using F1Predictor.Application.Features.Analysis.ExplainRacePrediction;
using F1Predictor.Application.Features.Championship.GetForecast;
using F1Predictor.Application.Features.Championship.GetScenarios;
using F1Predictor.Application.Features.Championship.GetStandings;
using F1Predictor.Application.Features.Predictions.PredictRace;
using F1Predictor.Application.Features.Predictions.PreviewNextRace;
using F1Predictor.Application.Features.Seasons.GetRaces;
using F1Predictor.Application.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class AskAnalystCommandHandlerTests
{
    private static AnalystTools Tools() => new(
        new StubQueryHandler<PreviewNextRaceQuery, NextRacePreviewResponse>(),
        new StubQueryHandler<ExplainDriverPredictionQuery, DriverExplanationResponse>(),
        new StubQueryHandler<GetStandingsQuery, StandingsResponse>(),
        new StubQueryHandler<GetChampionshipForecastQuery, ChampionshipForecastResponse>(),
        new StubQueryHandler<GetTitleScenariosQuery, TitleScenariosResponse>(),
        new StubQueryHandler<GetSeasonRacesQuery, IReadOnlyList<SeasonRaceResponse>>(),
        new StubQueryHandler<PredictRaceQuery, RacePredictionsResponse>(),
        new StubQueryHandler<ExplainRacePredictionQuery, DriverExplanationResponse>());

    private static AskAnalystCommandHandler Handler(FakeChatClient chat, bool aiAvailable = true, bool modelsAvailable = true) =>
        new(chat, new FakeAiCapabilities(aiAvailable), new FakeRacePredictor { ModelsAvailable = modelsAvailable },
            Tools(), new FakeDateTimeProvider(), NullLogger<AskAnalystCommandHandler>.Instance);

    [Fact]
    public async Task Handle_AiUnavailable_ReturnsAiUnavailable()
    {
        var result = await Handler(new FakeChatClient(), aiAvailable: false)
            .Handle(new AskAnalystCommand(2026, [new("user", "Who wins?")]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.AiUnavailable");
    }

    [Fact]
    public async Task Handle_ModelsNotTrained_ReturnsModelsNotTrained()
    {
        var result = await Handler(new FakeChatClient(), modelsAvailable: false)
            .Handle(new AskAnalystCommand(2026, [new("user", "Who wins?")]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Prediction.ModelsNotTrained");
    }

    [Fact]
    public async Task Handle_Available_StreamsDeltasAndDone()
    {
        var chat = new FakeChatClient
        {
            StreamFactory = () =>
            [
                new ChatResponseUpdate(ChatRole.Assistant, "Max "),
                new ChatResponseUpdate(ChatRole.Assistant, "leads.")
            ]
        };

        var result = await Handler(chat).Handle(new AskAnalystCommand(2026, [new("user", "Who leads?")]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var events = await result.Value.Events.ToListAsync();
        events.Select(e => e.Type).Should().Equal("delta", "delta", "done");

        chat.Calls.Should().HaveCount(1);
        chat.Calls[0][0].Role.Should().Be(ChatRole.System);
        chat.Calls[0][0].Text.Should().Contain("2026");
        chat.Options[0]!.Tools.Should().HaveCount(8);
    }

    [Fact]
    public async Task Handle_HistoryRoles_MapToChatRoles()
    {
        var chat = new FakeChatClient { StreamFactory = () => [new ChatResponseUpdate(ChatRole.Assistant, "ok")] };
        var messages = new List<ChatTurn> { new("user", "a"), new("assistant", "b"), new("user", "c") };

        var result = await Handler(chat).Handle(new AskAnalystCommand(2026, messages), CancellationToken.None);
        await result.Value.Events.ToListAsync();

        chat.Calls[0].Select(m => m.Role).Should().Equal(ChatRole.System, ChatRole.User, ChatRole.Assistant, ChatRole.User);
    }
}

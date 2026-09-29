using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.FindSimilarRaces;
using F1Predictor.Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class FindSimilarRacesQueryHandlerTests
{
    private static readonly FakeAiCapabilities EmbeddingsOn = new(chatAvailable: false, embeddingsAvailable: true);

    private static DateTimeOffset DateOf(int day) => new(2026, 6, day, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_RaceIndexedWithOthers_ReturnsSimilarRacesExcludingSourceWithPositiveSimilarity()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 1, meetingKey: 1, year: 2026, "Race A", "AAA", DateOf(1));
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 2, meetingKey: 2, year: 2026, "Race B", "BBB", DateOf(2));
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 3, meetingKey: 3, year: 2026, "Race C", "CCC", DateOf(3));

        var index = new FakeRaceEmbeddingIndex();
        await index.UpsertAsync(new RaceEmbeddingRecord(1, "m", 3, "hashA", "Text A", DateTimeOffset.UtcNow), new float[] { 1, 0, 0 }, CancellationToken.None);
        await index.UpsertAsync(new RaceEmbeddingRecord(2, "m", 3, "hashB", "Text B", DateTimeOffset.UtcNow), new float[] { 0.9f, 0.1f, 0 }, CancellationToken.None);
        await index.UpsertAsync(new RaceEmbeddingRecord(3, "m", 3, "hashC", "Text C", DateTimeOffset.UtcNow), new float[] { 0.1f, 1, 0 }, CancellationToken.None);

        var handler = new FindSimilarRacesQueryHandler(EmbeddingsOn, db, index);

        var result = await handler.Handle(new FindSimilarRacesQuery(1, Top: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SourceSessionKey.Should().Be(1);
        result.Value.SourceMeetingName.Should().Be("Race A");
        result.Value.Races.Should().HaveCount(2);
        result.Value.Races.Should().NotContain(r => r.SessionKey == 1);
        result.Value.Races.Should().OnlyContain(r => r.Similarity > 0 && r.Similarity <= 1);
        // Closer vector (0.9, 0.1, 0) ranks ahead of the near-orthogonal one (0.1, 1, 0).
        result.Value.Races[0].SessionKey.Should().Be(2);
    }

    [Fact]
    public async Task Handle_EmbeddingsUnavailable_ReturnsAiUnavailableEvenWhenIndexed()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 1, meetingKey: 1, year: 2026, "Race A", "AAA", DateOf(1));
        var index = new FakeRaceEmbeddingIndex();
        await index.UpsertAsync(new RaceEmbeddingRecord(1, "m", 3, "hashA", "Text A", DateTimeOffset.UtcNow), new float[] { 1, 0, 0 }, CancellationToken.None);
        var handler = new FindSimilarRacesQueryHandler(new FakeAiCapabilities(chatAvailable: false), db, index);

        var result = await handler.Handle(new FindSimilarRacesQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AnalysisErrors.AiUnavailable);
    }

    [Fact]
    public async Task Handle_SessionNeverIndexed_ReturnsNotIndexed()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 1, meetingKey: 1, year: 2026, "Race A", "AAA", DateOf(1));
        var index = new FakeRaceEmbeddingIndex();
        var handler = new FindSimilarRacesQueryHandler(EmbeddingsOn, db, index);

        var result = await handler.Handle(new FindSimilarRacesQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.NotIndexed");
    }

    [Fact]
    public async Task Handle_RaceIndexedButNoOtherRacesIndexed_ReturnsEmptyRacesWithoutError()
    {
        using var db = InMemoryDb.Create();
        await SeasonSeed.SeedClassifiedRaceAsync(db, sessionKey: 1, meetingKey: 1, year: 2026, "Race A", "AAA", DateOf(1));
        var index = new FakeRaceEmbeddingIndex();
        await index.UpsertAsync(new RaceEmbeddingRecord(1, "m", 3, "hashA", "Text A", DateTimeOffset.UtcNow), new float[] { 1, 0, 0 }, CancellationToken.None);

        var handler = new FindSimilarRacesQueryHandler(EmbeddingsOn, db, index);

        var result = await handler.Handle(new FindSimilarRacesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Races.Should().BeEmpty();
    }
}

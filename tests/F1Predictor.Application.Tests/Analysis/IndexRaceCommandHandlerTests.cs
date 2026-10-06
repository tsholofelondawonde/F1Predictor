using F1Predictor.Application.Features.Analysis;
using F1Predictor.Application.Features.Analysis.IndexRace;
using F1Predictor.Application.Tests.Fakes;
using F1Predictor.Infrastructure.Database;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class IndexRaceCommandHandlerTests
{
    private const int SessionKey = 500;

    private static async Task<ApplicationDbContext> SeedAsync()
    {
        var db = InMemoryDb.Create();
        await SeasonSeed.SeedClassifiedRaceAsync(
            db, SessionKey, meetingKey: 500, year: 2026, "Test GP", "TST",
            new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero));
        return db;
    }

    [Fact]
    public async Task Handle_EmbeddingsUnavailable_ReturnsAiUnavailableWithoutTouchingIndex()
    {
        using var db = await SeedAsync();
        var ai = new FakeAiCapabilities(chatAvailable: true, embeddingsAvailable: false);
        var embeddings = new FakeEmbeddingGenerator { ThrowIfCalled = true };
        var index = new FakeRaceEmbeddingIndex();
        var handler = new IndexRaceCommandHandler(db, ai, embeddings, index, new FakeDateTimeProvider());

        var result = await handler.Handle(new IndexRaceCommand { SessionKey = SessionKey }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AnalysisErrors.AiUnavailable);
        (await index.GetAsync(SessionKey, CancellationToken.None)).Should().BeNull();
    }

    // The whole point of the content-hash short-circuit: a second call with an unchanged fact
    // sheet and the same embedding model must not spend an embedding call re-writing the same
    // vector. The throwing generator is what proves it was never invoked, not just that the
    // response looked right.
    [Fact]
    public async Task Handle_SameContentHashAndModelAsStoredRecord_ShortCircuitsWithoutCallingEmbeddingGenerator()
    {
        using var db = await SeedAsync();
        var ai = new FakeAiCapabilities(chatAvailable: true, embeddingsAvailable: true, embeddingModel: "fake-embed-model");
        var index = new FakeRaceEmbeddingIndex();
        var clock = new FakeDateTimeProvider();

        var firstEmbeddings = new FakeEmbeddingGenerator();
        var firstHandler = new IndexRaceCommandHandler(db, ai, firstEmbeddings, index, clock);
        var first = await firstHandler.Handle(new IndexRaceCommand { SessionKey = SessionKey }, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Reindexed.Should().BeTrue();
        firstEmbeddings.CallCount.Should().Be(1);

        var throwingEmbeddings = new FakeEmbeddingGenerator { ThrowIfCalled = true };
        var secondHandler = new IndexRaceCommandHandler(db, ai, throwingEmbeddings, index, clock);
        var second = await secondHandler.Handle(new IndexRaceCommand { SessionKey = SessionKey }, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.Reindexed.Should().BeFalse();
        second.Value.ContentHash.Should().Be(first.Value.ContentHash);
    }

    // Same fact sheet, different embedding model: the stored vector was produced by a model that
    // is no longer configured, so it must be re-embedded even though nothing about the race changed.
    [Fact]
    public async Task Handle_ModelChangedSinceLastIndexed_ReEmbedsDespiteUnchangedContent()
    {
        using var db = await SeedAsync();
        var index = new FakeRaceEmbeddingIndex();
        var clock = new FakeDateTimeProvider();

        var oldAi = new FakeAiCapabilities(chatAvailable: true, embeddingsAvailable: true, embeddingModel: "old-model");
        var oldEmbeddings = new FakeEmbeddingGenerator();
        var oldHandler = new IndexRaceCommandHandler(db, oldAi, oldEmbeddings, index, clock);
        var first = await oldHandler.Handle(new IndexRaceCommand { SessionKey = SessionKey }, CancellationToken.None);
        first.Value.Reindexed.Should().BeTrue();

        var newAi = new FakeAiCapabilities(chatAvailable: true, embeddingsAvailable: true, embeddingModel: "new-model");
        var newEmbeddings = new FakeEmbeddingGenerator();
        var newHandler = new IndexRaceCommandHandler(db, newAi, newEmbeddings, index, clock);
        var second = await newHandler.Handle(new IndexRaceCommand { SessionKey = SessionKey }, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.Reindexed.Should().BeTrue();
        second.Value.ContentHash.Should().Be(first.Value.ContentHash); // The race's own facts never changed.
        newEmbeddings.CallCount.Should().Be(1);

        var stored = await index.GetAsync(SessionKey, CancellationToken.None);
        stored.Should().NotBeNull();
        stored.Model.Should().Be("new-model");
    }

    [Fact]
    public async Task Handle_UnknownSession_ReturnsRaceNotFound()
    {
        using var db = InMemoryDb.Create();
        var ai = new FakeAiCapabilities(chatAvailable: true, embeddingsAvailable: true);
        var handler = new IndexRaceCommandHandler(db, ai, new FakeEmbeddingGenerator(), new FakeRaceEmbeddingIndex(), new FakeDateTimeProvider());

        var result = await handler.Handle(new IndexRaceCommand { SessionKey = 999 }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.RaceNotFound");
    }
}

using F1Predictor.Application.Features.Seasons.Ingest;
using F1Predictor.Domain.RaceData.Entities;
using F1Predictor.Infrastructure.Database;
using F1Predictor.Infrastructure.Ingestion;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.Ingestion;

public sealed class SeasonIngestionCoordinatorJobTests
{
    private const int BufferMinutes = 180;
    private static readonly DateTime FixedUtcNow = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset Now = new(FixedUtcNow, TimeSpan.Zero);

    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options, domainEventsDispatcher: null);

    private static SeasonIngestionCoordinatorJob Job(
        ApplicationDbContext db, FakeDateTimeProvider? clock = null, FakeIngestSeasonCommandHandler? ingestHandler = null) =>
        new(db, ingestHandler ?? new FakeIngestSeasonCommandHandler(),
            Options.Create(new IngestionSchedulerOptions { PostSessionBufferMinutes = BufferMinutes }),
            clock ?? new FakeDateTimeProvider { UtcNow = FixedUtcNow },
            NullLogger<SeasonIngestionCoordinatorJob>.Instance);

    private static void AddMeetingAndSession(ApplicationDbContext db, int year, int sessionKey, RaceSession session)
    {
        db.Meetings.Add(new Meeting
        {
            MeetingKey = session.MeetingKey,
            Year = year,
            MeetingName = $"GP {sessionKey}",
            CircuitShortName = "A",
            CountryName = "X",
            DateStart = session.DateStart
        });
        db.RaceSessions.Add(session);
    }

    [Fact]
    public async Task DueYearsAsync_EmptyDatabase_ReportsNothingDue()
    {
        // The "seed an empty database" behaviour lives in Execute, not DueYearsAsync itself —
        // DueYearsAsync alone reports no years due when there is nothing in the database.
        using var db = CreateDb();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task DueYearsAsync_UnclassifiedSessionPastBuffer_IsDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 10, new RaceSession
        {
            SessionKey = 10,
            MeetingKey = 1,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddMinutes(-(BufferMinutes + 1)), // just past the buffer
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().ContainSingle().Which.Should().Be(2026);
    }

    [Fact]
    public async Task DueYearsAsync_UnclassifiedSessionWithinBuffer_IsNotDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 10, new RaceSession
        {
            SessionKey = 10,
            MeetingKey = 1,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddMinutes(-(BufferMinutes - 1)), // not past the buffer yet
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task DueYearsAsync_ClassifiedSessionPastBuffer_IsNotDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 10, new RaceSession
        {
            SessionKey = 10,
            MeetingKey = 1,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddMinutes(-(BufferMinutes + 1)),
            IsClassified = true
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    // --- The new qualifying due-rule (Task 15) ---

    [Fact]
    public async Task DueYearsAsync_QualifyingPastBufferAndNoGridStored_IsDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1), // the race itself is still in the future
            QualifyingDateStart = Now.AddMinutes(-(BufferMinutes + 1)), // but qualifying is over
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().ContainSingle().Which.Should().Be(2026);
    }

    [Fact]
    public async Task DueYearsAsync_QualifyingPastBufferButGridAlreadyStored_IsNotDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1),
            QualifyingDateStart = Now.AddMinutes(-(BufferMinutes + 1)),
            IsClassified = false
        });
        db.StartingGridEntries.Add(new StartingGridEntry { SessionKey = 20, DriverNumber = 1, Position = 1, LapDuration = 80.0 });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task DueYearsAsync_QualifyingWithinBuffer_IsNotDue()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1),
            QualifyingDateStart = Now.AddMinutes(-(BufferMinutes - 1)), // qualifying not "over" yet
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task DueYearsAsync_NoQualifyingDateStartYet_IsNotDueByQualifyingRule()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1),
            QualifyingDateStart = null,
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().BeEmpty();
    }

    [Fact]
    public async Task DueYearsAsync_BothRulesTriggerSameYear_ReportsYearOnce()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 10, new RaceSession
        {
            SessionKey = 10,
            MeetingKey = 1,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddMinutes(-(BufferMinutes + 1)),
            IsClassified = false
        });
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1),
            QualifyingDateStart = Now.AddMinutes(-(BufferMinutes + 1)),
            IsClassified = false
        });
        await db.SaveChangesAsync();

        var due = await Job(db).DueYearsAsync(CancellationToken.None);

        due.Should().ContainSingle().Which.Should().Be(2026);
    }

    [Fact]
    public async Task Execute_QualifyingDueRule_ReIngestsTheSeason()
    {
        using var db = CreateDb();
        AddMeetingAndSession(db, 2026, 20, new RaceSession
        {
            SessionKey = 20,
            MeetingKey = 2,
            SessionName = "Race",
            SessionType = "Race",
            DateStart = Now.AddDays(1),
            QualifyingDateStart = Now.AddMinutes(-(BufferMinutes + 1)),
            IsClassified = false
        });
        await db.SaveChangesAsync();
        var ingestHandler = new FakeIngestSeasonCommandHandler();

        await Job(db, ingestHandler: ingestHandler).Execute(new FakeJobExecutionContext());

        ingestHandler.HandledYears.Should().ContainSingle().Which.Should().Be(2026);
    }
}
